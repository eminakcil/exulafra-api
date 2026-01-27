namespace ExulofraApi.Common.Models;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    Unauthorized = 2,
    NotFound = 3,
    Conflict = 4,
    Forbidden = 5,
}

public record Error(ErrorType Type, string Message, object? ValidationErrors = null)
{
    public static Error Failure(string message) => new(ErrorType.Failure, message);

    public static Error NotFound(string message) => new(ErrorType.NotFound, message);

    public static Error Unauthorized(string message) => new(ErrorType.Unauthorized, message);

    public static Error Conflict(string message) => new(ErrorType.Conflict, message);
}

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; } // String yerine Error nesnesi

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T? value, bool isSuccess, Error? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, null);

    public static new Result<T> Failure(Error error) => new(default, false, error);
}
