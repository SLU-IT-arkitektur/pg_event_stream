using System.Text.Json;
using Dapper;
using Npgsql;

var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
if (string.IsNullOrEmpty(connectionString))
    connectionString = "Host=db;Database=eventsdb;Username=producer;Password=producer";

var connection = new NpgsqlConnection(connectionString);


Console.WriteLine("sleeping 10 seconds to let pg start up");
await Task.Delay(10000);


// every two seconds we send two events with different topics
var sql = "INSERT INTO events (name, topic, body, created_at) VALUES (@name, @topic, @body::jsonb, @created_at) RETURNING id;";
var everyTwoSeconds = new PeriodicTimer(TimeSpan.FromSeconds(2)); 
while (await everyTwoSeconds.WaitForNextTickAsync())
{
    var newCourseCreatedEvent = new CourseCreated(Guid.NewGuid(), "math", "Mathematics", 60, DateTime.UtcNow);
    var internalEvent = new InternalEvent(null, newCourseCreatedEvent.GetType().Name, "courses", JsonSerializer.Serialize(newCourseCreatedEvent), DateTime.UtcNow);
    var createdEventId = await connection.ExecuteScalarAsync(sql, internalEvent);
    await connection.ExecuteAsync($"NOTIFY events, '{internalEvent.topic}|{createdEventId}';"); 
    Console.WriteLine($"New event with id {createdEventId} and topic {internalEvent.topic} sent");

    var newPolicyUpdatedEvent = new PolicyUpdated(Guid.NewGuid(), "policy1", "Policy 1", DateTime.UtcNow);
    internalEvent = new InternalEvent(null, newPolicyUpdatedEvent.GetType().Name, "policies", JsonSerializer.Serialize(newPolicyUpdatedEvent), DateTime.UtcNow);
    var createdEventId2 = await connection.ExecuteScalarAsync(sql, internalEvent);
    await connection.ExecuteAsync($"NOTIFY events, '{internalEvent.topic}|{createdEventId2}';"); 
    Console.WriteLine($"New event with id {createdEventId2} and topic {internalEvent.topic} sent");

    // sending a bad formatted NOTIFICATION
    await connection.ExecuteAsync($"NOTIFY events, 'badformat';");
    
}

// internal event for our event stream 
record InternalEvent(int? id, string name, string topic, string body, DateTime created_at);
// external events from some external source
record CourseCreated(Guid CourseId, string Name, string Description, int Length, DateTime CreatedAt);
record PolicyUpdated(Guid PolicyId, string Name, string Description, DateTime CreatedAt);
