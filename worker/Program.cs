using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using MySqlConnector;

var natsUrl = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://localhost:4222";
var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "Server=localhost;Database=demo;User=root;Password=root;";

// Parse NATS URL to extract credentials (nats://user:pass@host:port)
var uri = new Uri(natsUrl);
var natsOpts = new NatsOpts
{
    Url = $"nats://{uri.Host}:{(uri.Port > 0 ? uri.Port : 4222)}"
};

if (!string.IsNullOrEmpty(uri.UserInfo))
{
    var parts = uri.UserInfo.Split(':');
    natsOpts = natsOpts with
    {
        AuthOpts = new NatsAuthOpts
        {
            Username = Uri.UnescapeDataString(parts[0]),
            Password = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : null
        }
    };
}

Console.WriteLine($"Connecting to NATS at {uri.Host}:{uri.Port}");
Console.WriteLine("Starting notification worker...");

await using var nats = new NatsConnection(natsOpts);
await nats.ConnectAsync();

var js = new NatsJSContext(nats);

// Ensure stream exists (lazy init - might already be created by PHP app)
try
{
    await js.CreateStreamAsync(new StreamConfig
    {
        Name = "notifications",
        Subjects = ["notifications"],
        Retention = StreamConfigRetention.Workqueue,
        Storage = StreamConfigStorage.File,
    });
    Console.WriteLine("Stream created.");
}
catch (NatsJSApiException e) when (e.Error.Code == 400)
{
    Console.WriteLine("Stream already exists.");
}

// Create durable consumer for work queue distribution
var consumer = await js.CreateOrUpdateConsumerAsync("notifications", new ConsumerConfig
{
    Name = "worker",
    DurableName = "worker",
});

Console.WriteLine("Connected to JetStream. Waiting for notifications...");

await foreach (var msg in consumer.ConsumeAsync<string>())
{
    var content = msg.Data ?? "";
    Console.WriteLine($"Received notification: {content}");

    try
    {
        await using var connection = new MySqlConnection(dbConnectionString);
        await connection.OpenAsync();

        await using var cmd = new MySqlCommand(
            "INSERT INTO notifications (content) VALUES (@content)",
            connection
        );
        cmd.Parameters.AddWithValue("@content", content);
        await cmd.ExecuteNonQueryAsync();

        await msg.AckAsync();
        Console.WriteLine("Notification saved to database.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database error: {ex.Message}");
    }
}
