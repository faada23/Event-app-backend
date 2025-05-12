public record EventFilterCriteriaRequest(
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    string? Location,
    string? CategoryName,
    string? EventName
);