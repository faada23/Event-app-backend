public class NotFoundException : Exception
{
    public string? EntityName { get; }
    public object? SearchedKey { get; }

    public NotFoundException()
        : base("The requested resource was not found.") { }

    public NotFoundException(string message)
        : base(message) { }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException) { }

    public NotFoundException(string entityName, object searchedKey)
        : base($"Entity '{entityName}' with key '{searchedKey}' was not found.")
    {
        EntityName = entityName;
        SearchedKey = searchedKey;
    }
}
