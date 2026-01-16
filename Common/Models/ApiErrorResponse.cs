namespace ExulofraApi.Common.Models;

public record ApiErrorResponse(int StatusCode, string Message, object? Errors = null);
