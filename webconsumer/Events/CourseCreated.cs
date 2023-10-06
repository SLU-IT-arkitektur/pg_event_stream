namespace Events;
public record CourseCreated(Guid CourseId, string Name, string Description, int Length, DateTime CreatedAt);