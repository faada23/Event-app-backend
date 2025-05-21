using Microsoft.AspNetCore.Identity;

public class PasswordHasherAdapter : IUserPasswordHasher
{
    private readonly IPasswordHasher<User> _identityPasswordHasher;

    public PasswordHasherAdapter(IPasswordHasher<User> identityPasswordHasher)
    {
        _identityPasswordHasher = identityPasswordHasher ?? throw new ArgumentNullException(nameof(identityPasswordHasher));
    }

    public string HashPassword(User user, string password)
    {
        return _identityPasswordHasher.HashPassword(user, password);
    }

    public PasswordVerificationStatus VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
    {
        var identityResult = _identityPasswordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);

        switch (identityResult)
        {
            case PasswordVerificationResult.Success:
                return PasswordVerificationStatus.Success;
            case PasswordVerificationResult.SuccessRehashNeeded:
                return PasswordVerificationStatus.SuccessRehashNeeded;
            case PasswordVerificationResult.Failed:
            default: 
                return PasswordVerificationStatus.Failed;
        }
    }
    
}