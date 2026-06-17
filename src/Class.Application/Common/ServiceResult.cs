namespace Class.Application.Common;

public sealed class ServiceResult<T>
{
    private ServiceResult(bool succeeded, T? value, ApiError? error, int statusCode)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
        StatusCode = statusCode;
    }

    public bool Succeeded { get; }

    public T? Value { get; }

    public ApiError? Error { get; }

    public int StatusCode { get; }

    public static ServiceResult<T> Ok(T value)
    {
        return new ServiceResult<T>(true, value, null, 200);
    }

    public static ServiceResult<T> Created(T value)
    {
        return new ServiceResult<T>(true, value, null, 201);
    }

    public static ServiceResult<T> Validation(string code, string message, object? details = null)
    {
        return Fail(code, message, 400, details);
    }

    public static ServiceResult<T> Unauthorized(string message = "Authentication is required.")
    {
        return Fail(ErrorCodes.Unauthorized, message, 401);
    }

    public static ServiceResult<T> Forbidden(string code, string message)
    {
        return Fail(code, message, 403);
    }

    public static ServiceResult<T> NotFound(string code, string message)
    {
        return Fail(code, message, 404);
    }

    public static ServiceResult<T> Conflict(string code, string message)
    {
        return Fail(code, message, 409);
    }

    public static ServiceResult<T> InternalError(string code, string message, object? details = null)
    {
        return Fail(code, message, 500, details);
    }

    private static ServiceResult<T> Fail(string code, string message, int statusCode, object? details = null)
    {
        return new ServiceResult<T>(false, default, new ApiError(code, message, details), statusCode);
    }
}
