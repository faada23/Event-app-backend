public class RefreshToken {
    public Guid Id {get;set;}
    public string Token {get;set;} = null!;
    public bool IsRevoked {get;set;} = false;
    public DateTimeOffset AddedDate {get;set;} = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiryDate {get;set;}

    public Guid UserId {get;set;}
    public User User {get;set;} = null!;
}