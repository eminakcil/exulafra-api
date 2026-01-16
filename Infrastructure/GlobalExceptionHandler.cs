using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using ExulofraApi.Common.Models;

namespace ExulofraApi.Infrastructure;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        // Serilog bunu otomatik olarak yakalar ve loglar
        logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var (statusCode, message, errors) = exception switch
        {
            // FluentValidation hatalarını yakalıyoruz
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                validationException
                    .Errors.GroupBy(x => x.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray())
            ),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized access",
                null
            ),

            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", null),
        };

        httpContext.Response.StatusCode = statusCode;

        var response = new ApiErrorResponse(statusCode, message, errors);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
