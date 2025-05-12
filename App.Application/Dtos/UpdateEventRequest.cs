public record UpdateEventRequest(
    string Name,
    string Description,
    DateTimeOffset DateTimeOfEvent,
    string Location,
    int MaxParticipants,
    Guid CategoryId 
);