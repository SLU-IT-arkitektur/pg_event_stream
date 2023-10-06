
using System.ComponentModel;
using System.Text.Json;
using Events;
using Features;

public class ApplicationEventHandler : IApplicationEventHandler
{
    private readonly IMongoDataStore _mongoDataStore;
    private readonly ILogger<ApplicationEventHandler> _logger;

    // needs all features or a mediator (see https://github.com/jbogard/MediatR for a good mediator implementation)
    private readonly ICreateCourse _createCourseFeature;

    public ApplicationEventHandler(IMongoDataStore mongoDataStore, ILogger<ApplicationEventHandler> logger, ICreateCourse createCourseFeature)
    {
        _mongoDataStore = mongoDataStore;
        _logger = logger;
        // features
        _createCourseFeature = createCourseFeature;
    }

    public async Task OnNotification(List<PGEvent> newEvents)
    {
        foreach (var newEvent in newEvents)
        {
            if (await TryClaimEvent(newEvent.id))
            {
                _logger.LogInformation($"Event {newEvent.id} claimed. Handling..");
                await HandledEvent(newEvent);

            }
            else
            {
                _logger.LogInformation($"Event {newEvent.id} already claimed. Ignoring.");
            }
        }
    }
    private async Task HandledEvent(PGEvent e)
    {
        switch (e.name)
        {
            // we catch exceptions in the subscriber and pause listening for new events
            case "CourseCreated":
                var courseCreated = JsonSerializer.Deserialize<CourseCreated>(e.body) ?? throw new FormatException($"Could not deserialize event body: {e.body}");
                await _createCourseFeature.ExecuteAsync(courseCreated); 
                break;
            default:
                _logger.LogInformation($"Unknown event type: {e.body}");
                break;
        }
    }
    private async Task<bool> TryClaimEvent(int eventId)
    {
        return await _mongoDataStore.TryMarkAsReceived(eventId);
    }

    public async Task<int> GetLatestReceivedId()
    {
        return await _mongoDataStore.GetLatestReceivedId();
    }
}