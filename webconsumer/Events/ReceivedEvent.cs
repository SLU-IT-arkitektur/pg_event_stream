using MongoDB.Bson;

public class ReceivedEvent
{
    public ObjectId Id { get; set; }
    public int eventId { get; set; }
    public DateTime receivedAt { get; set; }

    public ReceivedEvent(int eventId, DateTime receivedAt)
    {
        this.eventId = eventId;
        this.receivedAt = receivedAt;
    }
}