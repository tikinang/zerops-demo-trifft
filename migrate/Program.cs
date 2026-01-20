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

const int maxRetries = 3;
const int initialDelayMs = 1000;

for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Migration completed successfully.");
        return;
    }
    catch (MySqlException ex) when (attempt < maxRetries)
    {
        var delay = initialDelayMs * attempt;
        Console.WriteLine($"Attempt {attempt}/{maxRetries} failed: {ex.Message}");
        Console.WriteLine($"Retrying in {delay}ms...");
        await Task.Delay(delay);
    }
}

Console.Error.WriteLine("Migration failed after all retries.");
Environment.Exit(1);
