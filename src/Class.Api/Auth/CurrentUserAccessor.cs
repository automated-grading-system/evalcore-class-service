using System.Security.Claims;
using Class.Application.Abstractions;

namespace Class.Api.Auth;

public sealed class CurrentUserAccessor : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true && UserId != Guid.Empty;
        }
    }

    public Guid UserId
    {
        get
        {
            var value = FindClaim("sub") ?? FindClaim(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
        }
    }

    public string Email => FindClaim("email") ?? FindClaim(ClaimTypes.Email) ?? string.Empty;

    public string Role => (FindClaim("role") ?? FindClaim(ClaimTypes.Role) ?? string.Empty).ToLowerInvariant();

    public string? FullName => FindClaim("fullName") ?? FindClaim("name");

    private string? FindClaim(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
    }
}
