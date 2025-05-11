public record UserEventParticipationResponse(
    Guid UserId,
    Guid EventId,
    DateTimeOffset EventRegistrationDate
);