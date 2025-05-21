public interface IUserPasswordHasher
    {
        string HashPassword(User user, string password);
        PasswordVerificationStatus VerifyHashedPassword(User user, string hashedPassword, string providedPassword);
    }