public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Message { get; }
    public ErrorType? ErrorType { get; }

    private Result(T? value, bool isSuccess, string? message, ErrorType? errorType)
    {
        Value = value;
        IsSuccess = isSuccess;
        Message = message;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(value, true, null, null);
    }

    public static Result<T> Failure(string message, ErrorType errorType)
    {
        return new Result<T>(default, false, message, errorType);
    }

}