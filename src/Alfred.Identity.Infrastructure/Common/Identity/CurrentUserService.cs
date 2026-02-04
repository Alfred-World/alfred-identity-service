using System.Security.Claims;

using Alfred.Identity.Domain.Abstractions;

using Microsoft.AspNetCore.Http;

namespace Alfred.Identity.Infrastructure.Common.Identity;

/// <summary>
/// Implementation of ICurrentUser that retrieves user information from HTTP context claims
/// </summary>
public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = GetClaimValue(ClaimTypes.NameIdentifier)
                              ?? GetClaimValue("sub");

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Username => GetClaimValue(ClaimTypes.Name)
                               ?? GetClaimValue("preferred_username");

    public string? Email => GetClaimValue(ClaimTypes.Email)
                            ?? GetClaimValue("email");

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid GetRequiredUserId()
    {
        var userId = UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated or user ID not found in token");
        }

        return userId.Value;
    }

    private string? GetClaimValue(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
    }
}
