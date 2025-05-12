public class User {
    public Guid Id {get;set;}
    public string FirstName {get;set;} = null!;
    public string LastName {get;set;} = null!;
    public string Email {get;set;} = null!;
    public DateOnly DateOfBirth {get;set;}
    public string PasswordHash {get;set;} = null!;
    public DateTimeOffset SystemRegistrationDate {get;set;} = DateTimeOffset.UtcNow;

    public ICollection<Role> Roles {get;set;} = new List<Role>();
    public ICollection<EventParticipant>  EventParticipations {get;set;} = new List<EventParticipant>();
    public ICollection<RefreshToken> RefreshTokens {get;set;} = new List<RefreshToken>();
    
}