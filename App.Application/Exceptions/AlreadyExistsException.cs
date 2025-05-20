public class AlreadyExistsException : Exception{
    
    public string? EntityName { get; }
    public object? ConflictingKey { get; }

    public AlreadyExistsException()
        : base("The specified resource already exists.") {}
    public AlreadyExistsException(string message)
        : base(message) {}
    public AlreadyExistsException(string message, Exception innerException) 
        : base(message,innerException) {}

    public AlreadyExistsException(string entityName, object conflictingKey)
        : base($"Entity '{entityName}' with key '{conflictingKey}' already exists.")
    {
        EntityName = entityName;
        ConflictingKey = conflictingKey;
    }
} 