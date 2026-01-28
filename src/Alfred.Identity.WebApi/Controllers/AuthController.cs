using System.Security.Claims;
using System.Text.Json;

using Alfred.Identity.Application.Auth.Commands.Login;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.WebApi.Contracts.Auth;
using Alfred.Identity.WebApi.Contracts.Common;

using MediatR;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

/// <summary>
/// Handles SSO authentication flows.
/// Only supports SSO login mechanism - no direct login alternatives.
/// </summary>
[ApiController]
[Route("identity/auth")]
[Produces("application/json")]
public class AuthController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IAuthTokenService _authTokenService;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IConfiguration _configuration;

    public AuthController(
        IMediator mediator,
        IAuthTokenService authTokenService,
        IApplicationRepository applicationRepository,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _authTokenService = authTokenService;
        _applicationRepository = applicationRepository;
        _configuration = configuration;
    }

    /// <summary>
    /// SSO Login - validates credentials and returns exchange URL
    /// </summary>
    /// <remarks>
    /// This endpoint is used by the Identity UI for SSO flow.
    /// It validates credentials and returns a one-time token exchange URL.
    /// The client should navigate to the exchange URL to set the HttpOnly cookie.
    /// 
    /// Flow: Login API → Get Exchange Token → Browser navigates to Exchange URL → Cookie set
    /// </remarks>
    [HttpPost("sso-login")]
    [ProducesResponseType(typeof(ApiSuccessResponse<SsoLoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SsoLogin([FromBody] SsoLoginRequest request)
    {
        // 1. Validate credentials via CQRS
        var command = new LoginCommand(
            request.Identity,
            request.Password,
            request.RememberMe,
            GetClientIpAddress(),
            GetUserAgent()
        );

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return UnauthorizedResponse(result.Error ?? "Login failed");
        }

        var loginData = result.Value!;

        // 2. Generate One-Time-Token (OTP) for Cookie Exchange
        var authToken = await _authTokenService.GenerateTokenAsync(new AuthTokenData
        {
            UserId = loginData.User.Id,
            Email = loginData.User.Email,
            FullName = loginData.User.FullName,
            UserName = loginData.User.UserName,
            RememberMe = request.RememberMe,
            ExpiresAt = DateTime.UtcNow.AddSeconds(60)
        });

        // 3. Build Exchange URL
        var gatewayUrlConfig = _configuration["Urls:Gateway"] ?? "https://gateway.test";
        var forwardedHost = HttpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
        var scheme = HttpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? "https";
        
        // Build base URL - prefer X-Forwarded-Host, fallback to config
        string baseUrl;
        if (!string.IsNullOrEmpty(forwardedHost))
        {
            baseUrl = $"{scheme}://{forwardedHost}";
        }
        else
        {
            // Config may already include scheme (e.g., "https://gateway.test")
            baseUrl = gatewayUrlConfig.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? gatewayUrlConfig
                : $"{scheme}://{gatewayUrlConfig}";
        }

        var returnUrl = request.ReturnUrl ?? "/";
        var exchangeUrl =
            $"{baseUrl}/identity/auth/exchange-token?token={Uri.EscapeDataString(authToken)}&returnUrl={Uri.EscapeDataString(returnUrl)}";

        // 4. Return minimal response
        return OkResponse(new SsoLoginResponse
        {
            ReturnUrl = exchangeUrl,
            User = loginData.User
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
        // 1. Validate and consume the token (single-use)
        var tokenData = await _authTokenService.ValidateAndConsumeTokenAsync(token);
        if (tokenData == null)
        {
            return BadRequest(new
            {
                error = "invalid_token",
                error_description = "Token is invalid, expired, or has already been used"
            });
        }

        // 2. Create claims for cookie authentication
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
                : DateTimeOffset.UtcNow.AddHours(24),
            AllowRefresh = true
        };

        // 3. Sign in and set cookie (first-party context)
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties
        );

        // 4. Validate redirect URL against registered applications
        var redirectUrl = await ValidateAndGetRedirectUrlAsync(returnUrl);

        return Redirect(redirectUrl);
    }

    /// <summary>
    /// Get current session - verifies SSO cookie and returns user info
    /// </summary>
    [HttpGet("session")]
    [ProducesResponseType(typeof(ApiSuccessResponse<SsoSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult GetSession()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return UnauthorizedResponse("No valid session");
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr))
        {
            return UnauthorizedResponse("Invalid session data");
        }

        // Parse long instead of Guid (DB schema uses int8/long)
        if (!long.TryParse(userIdStr, out var userId))
        {
            return UnauthorizedResponse("Invalid user ID format in session");
        }

        return OkResponse(new SsoSessionResponse
        {
            IsAuthenticated = true,
            User = new SessionUserInfoDto
            {
                Id = userId,
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                FullName = User.FindFirst(ClaimTypes.Name)?.Value,
                UserName = User.FindFirst("username")?.Value
            }
        });
    }

    /// <summary>
    /// Check SSO session and redirect back to app with auth data
    /// Used for cross-domain SSO - browser redirects here, we check cookie, and redirect back with token
    /// </summary>
    /// <remarks>
    /// Flow:
    /// 1. App redirects browser to: gateway.test/identity/auth/check-sso?returnUrl=https://sso.test/...
    /// 2. This endpoint checks the AlfredSession cookie
    /// 3. If authenticated: generate one-time token and redirect back with token
    /// 4. If not authenticated: redirect back with error param
    /// </remarks>
    [HttpGet("check-sso")]
    public async Task<IActionResult> CheckSso([FromQuery] string returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            return BadRequest(new { error = "returnUrl is required" });
        }

        // Check if user is authenticated via SSO cookie
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userIdStr) && long.TryParse(userIdStr, out var userId))
            {
                // Generate one-time token for the app to exchange
                var authToken = await _authTokenService.GenerateTokenAsync(new AuthTokenData
                {
                    UserId = userId,
                    Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                    FullName = User.FindFirst(ClaimTypes.Name)?.Value,
                    UserName = User.FindFirst("username")?.Value,
                    RememberMe = true,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(60)
                });

                // Redirect back to app with token
                var separator = returnUrl.Contains('?') ? "&" : "?";
                var redirectUrl = $"{returnUrl}{separator}sso_token={Uri.EscapeDataString(authToken)}";
                return Redirect(redirectUrl);
            }
        }

        // Not authenticated - redirect back with error
        var errorSeparator = returnUrl.Contains('?') ? "&" : "?";
        return Redirect($"{returnUrl}{errorSeparator}sso_error=not_authenticated");
    }

    /// <summary>
    /// Validate SSO token and return user data without consuming it
    /// Used by frontend to get user data from sso_token before creating local session
    /// </summary>
    [HttpGet("validate-token")]
    public async Task<IActionResult> ValidateToken([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { success = false, error = "token is required" });
        }

        try
        {
            // Validate and consume the token (single-use)
            var tokenData = await _authTokenService.ValidateAndConsumeTokenAsync(token);
            if (tokenData == null)
            {
                return BadRequest(new { success = false, error = "Invalid or expired token" });
            }

            return Ok(new
            {
                success = true,
                result = new
                {
                    userId = tokenData.UserId,
                    email = tokenData.Email,
                    fullName = tokenData.FullName,
                    userName = tokenData.UserName
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
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

    #region Private Methods

    /// <summary>
    /// Validates the redirect URL against registered applications in DB.
    /// Only allows URLs that match RedirectUris of registered applications.
    /// </summary>
    private async Task<string> ValidateAndGetRedirectUrlAsync(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            return "/";
        }

        // Allow local URLs
        if (Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        // Allow OIDC authorize path (standard flow)
        if (returnUrl.Contains("/connect/authorize", StringComparison.OrdinalIgnoreCase))
        {
            // Extract client_id from returnUrl to validate against registered app
            var clientId = ExtractClientIdFromUrl(returnUrl);
            if (!string.IsNullOrEmpty(clientId))
            {
                var app = await _applicationRepository.GetByClientIdAsync(clientId);
                if (app is { IsActive: true })
                {
                    // Client is registered and active, allow the redirect
                    return returnUrl;
                }
            }

            // If can't validate client, still allow /connect/authorize (it will fail there if invalid)
            return returnUrl;
        }

        // For other URLs, check if they match any registered application's RedirectUris
        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            // Get all active applications and check their RedirectUris
            var isAllowed = await IsRedirectUriAllowedAsync(uri.ToString());
            if (isAllowed)
            {
                return returnUrl;
            }
        }

        // Not allowed - redirect to safe default
        return "/";
    }

    /// <summary>
    /// Check if the redirect URI is allowed based on registered applications
    /// </summary>
    private async Task<bool> IsRedirectUriAllowedAsync(string redirectUri)
    {
        // Query applications to find one that allows this redirect URI
        // This is a simplified check - in production you might want to cache this
        var applications = await _applicationRepository.GetAllAsync();

        foreach (var app in applications.Where(a => a.IsActive))
        {
            var allowedUris = ParseUriList(app.RedirectUris);
            if (allowedUris.Any(allowed => UriMatches(redirectUri, allowed)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Parse URI list from JSON array or space-delimited string
    /// </summary>
    private static List<string> ParseUriList(string? uriString)
    {
        if (string.IsNullOrEmpty(uriString))
        {
            return new List<string>();
        }

        // Try JSON array first
        if (uriString.TrimStart().StartsWith("["))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(uriString) ?? new List<string>();
            }
            catch
            {
                // Fall through to space-delimited
            }
        }

        // Space-delimited
        return uriString.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    /// <summary>
    /// Check if a URI matches an allowed pattern
    /// </summary>
    private static bool UriMatches(string uri, string allowedPattern)
    {
        // Exact match
        if (string.Equals(uri, allowedPattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if URIs have the same host
        if (Uri.TryCreate(uri, UriKind.Absolute, out var uriParsed) &&
            Uri.TryCreate(allowedPattern, UriKind.Absolute, out var allowedParsed))
        {
            return string.Equals(uriParsed.Host, allowedParsed.Host, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// Extract client_id from a URL query string
    /// </summary>
    private static string? ExtractClientIdFromUrl(string url)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                return query["client_id"];
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    #endregion
}
