using Models;
using MongoDB.Driver;

public interface IMongoDataStore
{
    Task<bool> TryMarkAsReceived(int eventId);
    Task<int> GetLatestReceivedId();
    Task CreateCourseAsync(Course course);

    Task<List<Course>> GetCoursesAsync();
}

public class MongoDataStore : IMongoDataStore
{
    private IMongoClient _client;
    private readonly ILogger<MongoDataStore> _logger;
    public MongoDataStore(ILogger<MongoDataStore> logger)
    {
        _logger = logger;
        var connectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "mongodb://mongo:password@localhost:27017";
        }
        _logger.LogInformation("Connecting to Mongo..");
        _client = new MongoClient(connectionString);
        _client.ListDatabaseNames();
        _logger.LogInformation("Successfully connected to Mongo.");
        // ensure unique index on eventId
        _client.GetDatabase("consumer").GetCollection<ReceivedEvent>("receivedEvents").Indexes.CreateOne(
            new CreateIndexModel<ReceivedEvent>(Builders<ReceivedEvent>.IndexKeys.Ascending("eventId"),
            new CreateIndexOptions() { Unique = true }));

    }
    public async Task<bool> TryMarkAsReceived(int eventId)
    {
        
        _logger.LogInformation($"Trying to mark event {eventId} as received..");
        var collection = _client.GetDatabase("consumer").GetCollection<ReceivedEvent>("receivedEvents");
        try
        {
            await collection.InsertOneAsync(new ReceivedEvent(eventId, DateTime.UtcNow));
            _logger.LogInformation($"Successfully marked event {eventId} as received");
            return true;
        }
        catch (MongoWriteException ex)
        {
            if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                _logger.LogInformation($"Event {eventId} already marked as received.");
                return false;
            }
            else
            {
                throw;
            }
        }
    }
    public async Task<int> GetLatestReceivedId()
    {
        var collection = _client.GetDatabase("consumer").GetCollection<ReceivedEvent>("receivedEvents");
        var latestEvent = await collection.Find(x => true).SortByDescending(x => x.eventId).FirstOrDefaultAsync();
        if (latestEvent == null)
            return 0; 

        return latestEvent.eventId;
    }

    public async Task CreateCourseAsync(Course course)
    {
        var collection = _client.GetDatabase("consumer").GetCollection<Course>("courses");
        await collection.InsertOneAsync(course);
    }

    public async Task<List<Course>> GetCoursesAsync()
    {
        var collection = _client.GetDatabase("consumer").GetCollection<Course>("courses");
        return await collection.Find(x => true).ToListAsync();
    }
}

