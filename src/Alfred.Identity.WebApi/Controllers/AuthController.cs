using Alfred.Identity.Application.Auth.Commands.Login;
using Alfred.Identity.WebApi.Contracts.Auth;
using Alfred.Identity.WebApi.Contracts.Common;

using MediatR;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace Alfred.Identity.WebApi.Controllers;


[ApiController]
[Route("identity/auth")]
[Produces("application/json")]
public class AuthController : BaseApiController
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// SSO Login - validates credentials and sets authentication cookie for SSO
    /// </summary>
    /// <remarks>
    /// This endpoint is used by the Identity UI for SSO flow.
    /// It validates credentials and sets an HttpOnly cookie that can be shared
    /// across subdomains (e.g., api.test and identity.test).
    /// </remarks>
    [HttpPost("sso-login")]
    [ProducesResponseType(typeof(ApiSuccessResponse<SsoLoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SsoLogin([FromBody] SsoLoginRequest request)
    {
        // Validate credentials using existing login logic
        var command = new LoginCommand(
            Identity: request.Identity,
            Password: request.Password,
            RememberMe: request.RememberMe,
            IpAddress: GetClientIpAddress(),
            DeviceName: GetUserAgent()
        );

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return UnauthorizedResponse(result.Error ?? "Login failed");
        }

        var loginData = result.Value;

        // Create claims for cookie authentication
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, loginData.User.Id.ToString()),
            new(ClaimTypes.Email, loginData.User.Email ?? ""),
            new("sub", loginData.User.Id.ToString())
        };

        if (!string.IsNullOrEmpty(loginData.User.FullName))
        {
            claims.Add(new Claim(ClaimTypes.Name, loginData.User.FullName));
        }

        if (!string.IsNullOrEmpty(loginData.User.UserName))
        {
            claims.Add(new Claim("username", loginData.User.UserName));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = request.RememberMe,
            ExpiresUtc = request.RememberMe 
                ? DateTimeOffset.UtcNow.AddDays(14) 
                : DateTimeOffset.UtcNow.AddHours(24)
        };

        // Sign in and set cookie
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties
        );

        return OkResponse(new SsoLoginResponse
        {
            ReturnUrl = request.ReturnUrl,
            User = loginData.User
        });
    }

    /// <summary>
    /// Get current session - verifies SSO cookie and returns user info
    /// </summary>
    /// <remarks>
    /// This endpoint is used by client apps to verify if user has a valid SSO session.
    /// It reads the HttpOnly cookie and returns user information if session is valid.
    /// </remarks>
    [HttpGet("session")]
    [ProducesResponseType(typeof(ApiSuccessResponse<SsoSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult GetSession()
    {
        // Check if user is authenticated via cookie
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return UnauthorizedResponse("No valid session");
        }

        // Extract user info from claims (set during SsoLogin)
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var fullName = User.FindFirst(ClaimTypes.Name)?.Value;
        var userName = User.FindFirst("username")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return UnauthorizedResponse("Invalid session data");
        }

        // Safely parse userId as GUID
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return UnauthorizedResponse("Invalid user ID format in session");
        }

        return OkResponse(new SsoSessionResponse
        {
            IsAuthenticated = true,
            User = new SessionUserInfo
            {
                Id = userGuid,
                Email = email ?? "",
                FullName = fullName,
                UserName = userName
            }
        });
    }

    /// <summary>
    /// SSO Logout - clears the authentication cookie
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return OkResponse(new { Success = true, Message = "Logged out successfully" });
    }
}

/// <summary>
/// Response for SSO Session check
/// </summary>
public record SsoSessionResponse
{
    public bool IsAuthenticated { get; init; }
    public SessionUserInfo? User { get; init; }
}

/// <summary>
/// User info for session response
/// </summary>
public record SessionUserInfo
{
    public Guid Id { get; init; }
    public string Email { get; init; } = "";
    public string? FullName { get; init; }
    public string? UserName { get; init; }
}

/// <summary>
/// Response for SSO Login
/// </summary>
public record SsoLoginResponse
{
    public string? ReturnUrl { get; init; }
    public UserInfo User { get; init; } = null!;
}

/// <summary>
/// Request for SSO Login
/// </summary>
public record SsoLoginRequest
{
    public required string Identity { get; init; }
    public required string Password { get; init; }
    public bool RememberMe { get; init; }
    public string? ReturnUrl { get; init; }
}
