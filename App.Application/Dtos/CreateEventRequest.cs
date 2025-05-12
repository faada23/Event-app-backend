public record CreateEventRequest(
    string Name,
    string Description,
    DateTimeOffset EventDate,
    string Location,
    int MaxParticipants,
    Guid CategoryId
);