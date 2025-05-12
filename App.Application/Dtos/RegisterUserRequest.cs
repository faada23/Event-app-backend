public record RegisterUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    DateOnly DateOfBirth
);