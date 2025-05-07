public class EventParticipant {
    public Guid EventId {get;set;}
    public Event Event {get;set;} = null!;

    public Guid UserId {get;set;}
    public User User {get;set;} = null!;

    public DateTimeOffset EventRegistrationDate {get;set;} = DateTimeOffset.UtcNow;
}