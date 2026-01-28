using ExulofraApi.Common.Models;

namespace ExulofraApi.Common.Extensions;

public static class ResultExtensions
{
    public static IResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.Ok();

        return MapError(result.Error!);
    }

    public static IResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return MapError(result.Error!);
    }

    private static IResult MapError(Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => Results.BadRequest(
                new ApiErrorResponse(400, error.Message, error.ValidationErrors)
            ),
            ErrorType.Unauthorized => Results.Json(
                new ApiErrorResponse(401, error.Message),
                statusCode: 401
            ),
            ErrorType.NotFound => Results.Json(
                new ApiErrorResponse(404, error.Message),
                statusCode: 404
            ),
            ErrorType.Conflict => Results.Json(
                new ApiErrorResponse(409, error.Message),
                statusCode: 409
            ),
            ErrorType.Forbidden => Results.Json(
                new ApiErrorResponse(403, error.Message),
                statusCode: 403
            ),
            _ => Results.BadRequest(new ApiErrorResponse(400, error.Message)),
        };
    }
}
