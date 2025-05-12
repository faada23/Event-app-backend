public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth
);