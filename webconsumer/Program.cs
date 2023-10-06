using Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMongoDataStore, MongoDataStore>();
builder.Services.AddTransient<IApplicationEventHandler, ApplicationEventHandler>();
// features (if we dont use a mediator, see https://github.com/jbogard/MediatR for a good mediator implementation)
builder.Services.AddTransient<ICreateCourse, CreateCourse>();
builder.Services.AddTransient<IGetAllCourses, GetAllCourses>();

builder.Services.AddHostedService<PostgresEventsSubscriber>();
builder.Logging.AddConsole();

var app = builder.Build();


// endpoints
app.MapGet("/courses", async (IGetAllCourses getAllCoursesFeature) =>
{
    var courses = await getAllCoursesFeature.ExecuteAsync();
    return Results.Ok(courses);
});


app.Run("http://*:1337");
