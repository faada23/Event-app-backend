public record GetUserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    DateTimeOffset SystemRegistrationDate
);