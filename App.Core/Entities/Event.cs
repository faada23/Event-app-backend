public class Event{
    public Guid Id {get;set;}
    public string Name {get;set;} = null!;
    public string Description {get;set;} = null!;
    public DateTimeOffset DateTimeOfEvent {get;set;}
    public string Location {get;set;} = null!;
    public int MaxParticipants {get;set;}

    public Guid CategoryId {get;set;}
    public Category Category {get;set;} = null!;

    public Image? Image {get;set;}

    public ICollection<EventParticipant> EventParticipants {get;set;} = new List<EventParticipant>();
}