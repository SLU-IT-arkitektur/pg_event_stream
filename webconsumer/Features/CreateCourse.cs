using Events;
using Models;

namespace Features;


public interface ICreateCourse
{
    Task ExecuteAsync(CourseCreated createCourseEvent);
}
public class CreateCourse : ICreateCourse
{
    private readonly IMongoDataStore _dataStore;
    private readonly ILogger<CreateCourse> _logger;

    public CreateCourse(IMongoDataStore dataStore, ILogger<CreateCourse> logger)
    {
        _dataStore = dataStore;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(CourseCreated createCourseEvent)
    {
        _logger.LogInformation($"Creating course {createCourseEvent.CourseId}..");
        var newCourse = new Course()
        {
            CourseId = createCourseEvent.CourseId,
            Name = createCourseEvent.Name,
            Description = createCourseEvent.Description,
            Length = createCourseEvent.Length,
            CreatedAt = createCourseEvent.CreatedAt,
            CreatedBy = "LADOK-pgeventstream-listener" 
        };
        await _dataStore.CreateCourseAsync(newCourse);
    }

}
