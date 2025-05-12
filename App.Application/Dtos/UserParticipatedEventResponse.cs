public record UserParticipatedEventResponse(
    Guid EventId,
    string EventName,
    DateTimeOffset DateTimeOfEvent,
    string? Location 

);