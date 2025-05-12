public record GetEventResponse(
    Guid Id,
    string Name,
    string Description,
    DateTimeOffset DateTimeOfEvent,
    string Location,
    int MaxParticipants,
    GetCategoryResponse Category,
    EventImageDetailsResponse? Image
);