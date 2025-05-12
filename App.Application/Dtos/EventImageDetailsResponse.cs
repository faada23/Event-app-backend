public record EventImageDetailsResponse(
    Guid ImageId,
    string StoredPath,
    string ContentType,
    DateTimeOffset UploadedAt
);