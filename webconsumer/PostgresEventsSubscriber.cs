using Dapper;
using Npgsql;

public interface IApplicationEventHandler
{
    Task OnNotification(List<PGEvent> newEvents);
    Task<int> GetLatestReceivedId();
}
public class PostgresEventsSubscriber : BackgroundService
{
    private readonly CancellationTokenSource _emergencyCancellationToken = new();
    private readonly List<string> topics = new();
    private readonly ILogger<PostgresEventsSubscriber> _logger;
    private readonly IApplicationEventHandler _applicationEventHandler;

    private readonly string _connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING") ??
                                            "Host=localhost;Database=eventsdb;Username=consumer;Password=consumer";

    public PostgresEventsSubscriber(ILogger<PostgresEventsSubscriber> logger, IApplicationEventHandler applicationEventHandler)
    {
        _logger = logger;
        _applicationEventHandler = applicationEventHandler;

        var topicsAsCommaSeparatedString = Environment.GetEnvironmentVariable("TOPICS");
        if (string.IsNullOrEmpty(topicsAsCommaSeparatedString))
            topicsAsCommaSeparatedString = "courses, policies";


        topics = topicsAsCommaSeparatedString.Split(",").Select(t => t.Trim()).ToList();
        foreach (var topic in topics)
        {
            _logger.LogInformation($"Subscribing to topic: {topic}");
        }

    }

    protected async override Task ExecuteAsync(CancellationToken cancellingToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellingToken, _emergencyCancellationToken.Token);
        var linkedToken = linkedCts.Token; // this token will be cancelled if either of the two tokens are cancelled
        await StartListeningForEvents(linkedToken);
    }

    private async Task StartListeningForEvents(CancellationToken cancellationToken)
    {
        using var subscriptionConnection = await ConnectWithRetry(_connectionString); // subscribing to notifications requires an open connection

        // make sure we are up to date before we start listening for events!
        // even tho we are not using the long-lived subscriptionConnection (that is doing the LISTEN) for fetching events, we want to wait until it is established..
        // ... that way we know that it is safe to try and pre-fetch missed events 
        _logger.LogInformation("Making sure we are up to date..");
        var latestId = await _applicationEventHandler.GetLatestReceivedId();
        foreach (var topic in topics)
        {
            var events = await GetNewEvents(topic, latestId);
            _logger.LogInformation($"Fetched {events.Count()} new events for topic {topic}");
            await ProcessEventsOrStopListeningAsync(events);
        }

        // subscribe to notifications (i.e listen for events)
        subscriptionConnection.Notification += async (o, e) =>
        {
            var (successfullyParsedPayload, topic, eventId) = TryParsePayload(e.Payload);
            if (!successfullyParsedPayload)
                return;

            await HandleEvent(topic, eventId);
        };

        var channel = "events";
        await subscriptionConnection.ExecuteAsync($"LISTEN {channel};");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await subscriptionConnection.WaitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while waiting for notifications: {ex.Message}");
                _logger.LogInformation("Will try to reconnect in 10 seconds...");
                await Task.Delay(10_000);
                _logger.LogInformation("Trying to reconnect...");
                await StartListeningForEvents(cancellationToken);
            }

        }
    }

    private (bool, string, int) TryParsePayload(string payload)
    {
        try
        {
            // payload = topic|eventid
            var split = payload.Split("|");
            var topic = split[0];
            var eventId = int.Parse(split[1]);
            return (true, topic, eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error while parsing notification msg string, expecting topic|eventid: {ex.Message}");
            return (false, "", 0);
        }
    }
    
    // if an exception occurs while handling an event, we need to stop listening to notifications
    // we do not want to exit the application, because we dont want the hosting platform (k8s forexample) to automatically restart 
    // the app and thereby continue processing future events before dealing with the failed one...
    private async Task ProcessEventsOrStopListeningAsync(List<PGEvent> events)
    {
        try
        {
            await _applicationEventHandler.OnNotification(events);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while handling event");
            _logger.LogInformation("Application needs to be restarted in order to continue processing events.");
            _emergencyCancellationToken.Cancel();
        }
    }
    private async Task HandleEvent(string topic, int eventId)
    {
        if (topics.Contains(topic))
        {
            _logger.LogInformation($"Handling event for topic: {topic} and id: {eventId}");
            var latestId = await _applicationEventHandler.GetLatestReceivedId();
            _logger.LogInformation($"Latest processed event id: {latestId}");
            var events = await GetNewEvents(topic, latestId);
            _logger.LogInformation($"Fetched {events.Count()} new events for topic {topic}");

            await ProcessEventsOrStopListeningAsync(events);

        }
        else
        {
            _logger.LogInformation($"Ignoring event for topic: {topic}");
        }
    }
    private async Task<List<PGEvent>> GetNewEvents(string topic, int latestId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var newEventsSql = $"select * from events where id > @fetchFromEventId and topic = @topic order by id asc limit 60000;";

        var fetchFromEventId = latestId - 10; // we fetch 10 events before the latest processed event to make sure we dont miss any events under high load
        var events = await connection.QueryAsync<PGEvent>(newEventsSql, new { fetchFromEventId, topic });
        return events.ToList();
    }

    // this will retry connecting to the events database (with progressive back-off) 
    // for 3 hours and 24 minutes before giving up. The reason for this is because it will 
    // be triggered not only when the application starts, but also when the database becomes unavailable
    // (for example when the database is being restarted as part of patching the database server)
    private async Task<NpgsqlConnection> ConnectWithRetry(string connectionString)
    {
        const int maxRetryAttempts = 50;
        const int initialWaitTimeInMs = 10_000;

        for (int retryAttempt = 0; retryAttempt < maxRetryAttempts; retryAttempt++)
        {
            try
            {
                NpgsqlConnection connection = new(connectionString);
                _logger.LogInformation($"Connecting to events database (attempt {retryAttempt + 1}/{maxRetryAttempts})");
                await connection.OpenAsync();
                _logger.LogInformation("Connected to events database successfullyParsedPayloadfully!");
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not connect to events database, failed with: {ex.Message}");
                if (retryAttempt <= maxRetryAttempts)
                {
                    int waitTimeInMs = initialWaitTimeInMs * (retryAttempt + 1); // progressive back-off
                    _logger.LogInformation($"Waiting {waitTimeInMs}ms before retrying to connect to database");
                    await Task.Delay(waitTimeInMs);
                }
            }
        }

        _logger.LogError("Exceeded maximum retry attempts. Could not connect to events database.");
        throw new Exception("Could not connect to events database");
    }

}
public record PGEvent(int id, string name, string topic, string body, DateTime created_at);
