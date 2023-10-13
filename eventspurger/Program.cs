using Dapper;
using Npgsql;

var (succeeded, unit, number) = ArgsParser.TryGetUnitAndNumber(args);
if (!succeeded)
    Environment.Exit(1);

var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
if (string.IsNullOrEmpty(connectionString)) // TODO needs its own user and permissions
    connectionString = "Host=localhost;Database=eventsdb;Username=postgres;Password=password";

using var connection = new NpgsqlConnection(connectionString);
DateTime TargetTimestamp = unit.ToLower() switch 
{
    "minutes" => DateTime.Now.AddMinutes(-number),
    "hours" => DateTime.Now.AddHours(-number),
    "days" => DateTime.Now.AddDays(-number),
    _ => throw new ArgumentException($"invalid unit: {unit}")
};
Console.WriteLine($"purging events older than {number} {unit} from the events table");
var sql = $"DELETE FROM events WHERE created_at < @TargetTimestamp;";
var numberOfDeletedEvents = await connection.ExecuteAsync(sql, new { TargetTimestamp });
Console.WriteLine($"deleted {numberOfDeletedEvents} events");
