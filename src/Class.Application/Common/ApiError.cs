using System.Text.Json.Serialization;

namespace Class.Application.Common;

public sealed class ApiError
{
    public ApiError(string code, string message, object? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }

    public string Code { get; }

    public string Message { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Details { get; }
}
