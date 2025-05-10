using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

public static class ResultExtensions
{
    public static ActionResult ToActionResult<T>(this Result<T> result)
    {
        return result switch
        {
            { IsSuccess: true } => new OkObjectResult(result.Value),
            { ErrorType: ErrorType.AlreadyExists } => new ConflictObjectResult(result.Message),
            { ErrorType: ErrorType.RecordNotFound } => new NotFoundObjectResult(result.Message),
            { ErrorType: ErrorType.InvalidInput } => new BadRequestObjectResult(result.Message),
            { ErrorType: ErrorType.Forbidden} => new ObjectResult(result.Message) { StatusCode = 403 },
            _ => new ObjectResult(result.Message) { StatusCode = 500 }
        };
    }
}