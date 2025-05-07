public class Image {
    public Guid Id {get;set;}
    public string StoredPath {get;set;} = null!;
    public string ContentType {get;set;} = null!;
    public DateTimeOffset UploadedAt {get;set;} = DateTimeOffset.UtcNow;

    public Guid EventId { get; set; } 
    public Event Event { get; set; } = null!;
}