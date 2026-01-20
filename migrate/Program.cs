using MySqlConnector;

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
if (string.IsNullOrEmpty(connectionString))
{
    Console.Error.WriteLine("Error: DB_CONNECTION_STRING environment variable is not set");
    Environment.Exit(1);
}

var schemaPath = Path.Combine(AppContext.BaseDirectory, "schema.sql");
if (!File.Exists(schemaPath))
{
    Console.Error.WriteLine($"Error: schema.sql not found at {schemaPath}");
    Environment.Exit(1);
}

var sql = await File.ReadAllTextAsync(schemaPath);
Console.WriteLine("Running migration...");

await using var connection = new MySqlConnection(connectionString);
await connection.OpenAsync();

await using var command = new MySqlCommand(sql, connection);
await command.ExecuteNonQueryAsync();

Console.WriteLine("Migration completed successfully.");
