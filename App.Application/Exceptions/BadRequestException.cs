public class BadRequestException : Exception
{
    public string? ErrorCode { get; }

    public BadRequestException()
        : base("The request is invalid or malformed.") { }

    public BadRequestException(string message)
        : base(message) { }

    public BadRequestException(string message, Exception innerException)
        : base(message, innerException) { }

    public BadRequestException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

}