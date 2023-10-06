using Models;

namespace Features;

public interface IGetAllCourses
{
    Task<IEnumerable<Course>> ExecuteAsync();
}
public class GetAllCourses : IGetAllCourses
{
    private readonly IMongoDataStore _dataStore;
    private readonly ILogger<GetAllCourses> _logger;

    public GetAllCourses(IMongoDataStore dataStore, ILogger<GetAllCourses> logger)
    {
        _dataStore = dataStore;
        _logger = logger;
    }
    public async Task<IEnumerable<Course>> ExecuteAsync()
    {
        _logger.LogInformation($"Getting all courses..");
        return await _dataStore.GetCoursesAsync();
    }
}