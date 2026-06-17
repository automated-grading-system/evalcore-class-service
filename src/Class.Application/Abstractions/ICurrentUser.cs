namespace Class.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid UserId { get; }

    string Email { get; }

    string Role { get; }

    string? FullName { get; }
}
