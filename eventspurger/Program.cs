using Dapper;
using Npgsql;

var (succeeded, unit, number) = ArgsParser.TryGetUnitAndNumber(args);
if (!succeeded)
    Environment.Exit(1);

var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
if (string.IsNullOrEmpty(connectionString)) // TODO needs its own user and permissions
    connectionString = "Host=localhost;Database=eventsdb;Username=postgres;Password=password";

using var connection = new NpgsqlConnection(connectionString);

Console.WriteLine($"purging events table from events older than {number} {unit}");

var numberOfDeletedEvents = await connection.ExecuteAsync($"DELETE FROM events WHERE created_at < NOW() - INTERVAL '{number} {unit}';");

Console.WriteLine($"deleted {numberOfDeletedEvents} events");

