using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception caught by global handler");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
         int statusCode = StatusCodes.Status500InternalServerError;
        string userMessage = "Internal server error";
        string detail = ex.Message;

        switch (ex)
        {
            case AlreadyExistsException alreadyExistsEx:
                statusCode = StatusCodes.Status409Conflict;
                userMessage = "Resource already exists.";
                detail = alreadyExistsEx.Message;
                break;

            case BadRequestException badRequestEx:
                statusCode = StatusCodes.Status400BadRequest;
                userMessage = "Bad request.";
                detail = badRequestEx.Message;
                break;

            case NotFoundException notFoundEx:
                statusCode = StatusCodes.Status404NotFound;
                userMessage = "Resource not found.";
                detail = notFoundEx.Message;
                break;

            case ArgumentNullException argNullEx:
                statusCode = StatusCodes.Status400BadRequest;
                userMessage = "A required argument was null.";
                detail = argNullEx.Message;
                break;

            case ArgumentException argEx:
                statusCode = StatusCodes.Status400BadRequest;
                userMessage = "An invalid argument was provided.";
                detail = argEx.Message;
                break;

            case DbUpdateConcurrencyException concurrencyEx:
                statusCode = StatusCodes.Status409Conflict;
                userMessage = "A concurrency error occurred.";
                detail = concurrencyEx.Message;
                break;

            case DbUpdateException updateEx:
                statusCode = StatusCodes.Status500InternalServerError;
                userMessage = "A database update error occurred.";
                detail = updateEx.Message;
                break;

            default:
                break;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            message = userMessage,
            detail
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}