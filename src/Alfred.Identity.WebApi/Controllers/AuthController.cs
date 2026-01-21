using Alfred.Identity.Application.Auth.Commands.Login;
using Alfred.Identity.WebApi.Contracts.Auth;
using Alfred.Identity.WebApi.Contracts.Common;
using Alfred.Identity.WebApi.Services;

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
    private readonly IAuthTokenService _authTokenService;

    public AuthController(IMediator mediator, IAuthTokenService authTokenService)
    {
        _mediator = mediator;
        _authTokenService = authTokenService;
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

        // Generate a one-time auth token for cookie exchange
        // This token will be exchanged for a cookie via browser navigation (first-party context)
        var authToken = _authTokenService.GenerateToken(new AuthTokenData
        {
            UserId = loginData.User.Id,
            Email = loginData.User.Email,
            FullName = loginData.User.FullName,
            UserName = loginData.User.UserName,
            RememberMe = request.RememberMe,
            ExpiresAt = DateTime.UtcNow.AddSeconds(60) // Token valid for 60 seconds only
        });

        // Build the exchange URL - browser will navigate here to get the cookie
        var gatewayUrl = HttpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? "gateway.test";
        var scheme = HttpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? "https";
        var exchangeUrl = $"{scheme}://{gatewayUrl}/identity/auth/exchange-token?token={Uri.EscapeDataString(authToken)}&returnUrl={Uri.EscapeDataString(request.ReturnUrl ?? "")}";

        return OkResponse(new SsoLoginResponse
        {
            ReturnUrl = exchangeUrl,
            User = loginData.User,
            ExchangeToken = authToken
        });
    }

    /// <summary>
    /// Exchange one-time auth token for session cookie - browser navigates here directly
    /// </summary>
    /// <remarks>
    /// This endpoint is called via browser navigation (not CORS fetch) so the cookie
    /// is set in first-party context and won't be blocked by third-party cookie restrictions.
    /// </remarks>
    [HttpGet("exchange-token")]
    public async Task<IActionResult> ExchangeToken([FromQuery] string token, [FromQuery] string? returnUrl)
    {
        // Validate and consume the token
        var tokenData = _authTokenService.ValidateAndConsumeToken(token);
        if (tokenData == null)
        {
            return BadRequest(new { error = "invalid_token", error_description = "Token is invalid, expired, or has already been used" });
        }



        // Create claims for cookie authentication
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, tokenData.UserId.ToString()),
            new(ClaimTypes.Email, tokenData.Email ?? ""),
            new("sub", tokenData.UserId.ToString())
        };

        if (!string.IsNullOrEmpty(tokenData.FullName))
        {
            claims.Add(new Claim(ClaimTypes.Name, tokenData.FullName));
        }

        if (!string.IsNullOrEmpty(tokenData.UserName))
        {
            claims.Add(new Claim("username", tokenData.UserName));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = tokenData.RememberMe,
            ExpiresUtc = tokenData.RememberMe 
                ? DateTimeOffset.UtcNow.AddDays(14) 
                : DateTimeOffset.UtcNow.AddHours(24)
        };

        // Sign in and set cookie - this is first-party context!
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties
        );

        // Redirect to the original returnUrl (e.g., gateway.test/connect/authorize)
        var redirectUrl = returnUrl ?? "/";
        
        return Redirect(redirectUrl);
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
            User = new SessionUserInfoDto
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
