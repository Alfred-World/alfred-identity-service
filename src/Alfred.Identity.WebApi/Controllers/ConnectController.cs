using System.Security.Claims;
using System.Text.Json;

using Alfred.Identity.Application.Auth.Commands.Authorize;
using Alfred.Identity.Application.Auth.Commands.ExchangeCode;
using Alfred.Identity.Application.Auth.Common;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.WebApi.Configuration;
using Alfred.Identity.WebApi.Contracts.Connect;

using MediatR;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

/// <summary>
/// OIDC/OAuth2 Connect endpoints
/// </summary>
[ApiController]
[Route("connect")]
public class ConnectController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly AppConfiguration _appConfig;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;

    public ConnectController(
        IMediator mediator,
        ICurrentUser currentUser,
        AppConfiguration appConfig,
        IApplicationRepository applicationRepository,
        IUserRepository userRepository)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _appConfig = appConfig;
        _applicationRepository = applicationRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// OAuth2/OIDC Authorize Endpoint
    /// </summary>
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize([FromQuery] AuthorizeRequest request)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
        {
            if (request.prompt == "none")
            {
                return BadRequest(new { error = "login_required" });
            }

            var gatewayUrl = _appConfig.GatewayUrl;
            var forwardedHost = Request.Headers["X-Forwarded-Host"].FirstOrDefault();
            var forwardedProto = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? "https";

            string returnUrl;
            if (!string.IsNullOrEmpty(forwardedHost))
            {
                // Use the forwarded host (actual domain the user hit, e.g. gateway.lucasvu.io.vn)
                returnUrl = $"{forwardedProto}://{forwardedHost}{Request.Path}{Request.QueryString}";
            }
            else
            {
                returnUrl = $"{gatewayUrl}{Request.Path}{Request.QueryString}";
            }

            var ssoUrl = _appConfig.SsoWebUrl;
            var loginUrl = $"{ssoUrl}/login?returnUrl={Uri.EscapeDataString(returnUrl)}";

            return Redirect(loginUrl);
        }

        var userIdClaim = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? authenticateResult.Principal.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return BadRequest(new { error = "invalid_user" });
        }

        var ipAddress = GetClientIpAddress();
        var device = TruncateDevice(Request.Headers["User-Agent"].FirstOrDefault());

        var command = new AuthorizeCommand(
            request.client_id,
            request.redirect_uri,
            request.response_type,
            request.scope,
            request.state,
            request.code_challenge,
            request.code_challenge_method,
            request.prompt,
            userId,
            ipAddress,
            device
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            // Per RFC 6749 §4.1.2.1: if redirect_uri is invalid, do NOT redirect to it.
            // Instead redirect to SSO login page with error details for user-friendly display.
            var ssoUrl = _appConfig.SsoWebUrl;
            var errorParams = $"error={Uri.EscapeDataString(result.Error ?? "server_error")}" +
                              $"&error_description={Uri.EscapeDataString(result.ErrorDescription ?? "Authorization failed")}";

            return Redirect($"{ssoUrl}/login?{errorParams}");
        }

        return Redirect(result.RedirectLocation!);
    }

    /// <summary>
    /// OAuth2/OIDC Token Endpoint
    /// </summary>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Token([FromForm] ExchangeCodeRequest request)
    {
        var ipAddress = GetClientIpAddress();
        var device = TruncateDevice(Request.Headers["User-Agent"].FirstOrDefault());

        var command = new ExchangeCodeCommand(
            request.grant_type,
            request.client_id,
            request.client_secret,
            request.code,
            request.redirect_uri,
            request.code_verifier,
            request.refresh_token,
            ipAddress,
            device
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error, error_description = result.ErrorDescription });
        }

        return Ok(new TokenResponseDto
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            IdToken = result.IdToken,
            TokenType = result.TokenType ?? "Bearer",
            ExpiresIn = result.ExpiresIn
        });
    }

    /// <summary>
    /// OIDC UserInfo Endpoint - returns user claims based on access token scopes
    /// </summary>
    /// <remarks>
    /// Returns user information based on scopes granted:
    /// - openid: sub (user ID)
    /// - profile: name, username
    /// - email: email, email_verified
    /// </remarks>
    [HttpGet("userinfo")]
    [Authorize]
    public async Task<IActionResult> UserInfo()
    {
        if (_currentUser.UserId == null)
        {
            return Unauthorized(new { error = "invalid_token" });
        }

        // Get user from database
        var user = await _userRepository.GetByIdAsync(_currentUser.UserId!.Value, HttpContext.RequestAborted);
        if (user == null)
        {
            return NotFound(new { error = "user_not_found" });
        }

        // Build response based on scopes in the token
        var scopes = User.FindFirst("scope")?.Value?.Split(' ') ?? Array.Empty<string>();

        var userInfo = new Dictionary<string, object>
        {
            ["sub"] = _currentUser.UserId.Value.ToString()
        };

        // Profile scope: name, preferred_username
        if (scopes.Contains("profile") || scopes.Contains("openid"))
        {
            if (!string.IsNullOrEmpty(user.FullName))
            {
                userInfo["name"] = user.FullName;
            }

            if (!string.IsNullOrEmpty(user.UserName))
            {
                userInfo["preferred_username"] = user.UserName;
            }
        }

        // Email scope: email, email_verified
        if (scopes.Contains("email"))
        {
            userInfo["email"] = user.Email;
            userInfo["email_verified"] = user.EmailConfirmed;
        }

        return Ok(userInfo);
    }

    /// <summary>
    /// OIDC Logout / End Session Endpoint with validation
    /// </summary>
    [HttpGet("logout")]
    public async Task<IActionResult> Logout(
        [FromQuery] string? client_id,
        [FromQuery] string? post_logout_redirect_uri,
        [FromQuery] string? id_token_hint,
        [FromQuery] string? state)
    {
        // Sign out from cookie authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Validate post_logout_redirect_uri against registered URIs
        if (!string.IsNullOrEmpty(post_logout_redirect_uri))
        {
            var isValid = await ValidatePostLogoutRedirectUriAsync(client_id, post_logout_redirect_uri);
            if (!isValid)
            {
                return BadRequest(new
                {
                    error = "invalid_request",
                    error_description = "Invalid post_logout_redirect_uri"
                });
            }

            var redirectUrl = post_logout_redirect_uri;
            if (!string.IsNullOrEmpty(state))
            {
                redirectUrl += (redirectUrl.Contains('?') ? "&" : "?") + $"state={state}";
            }

            return Redirect(redirectUrl);
        }

        // Default redirect to SSO login
        var ssoUrl = _appConfig.SsoWebUrl;
        return Redirect($"{ssoUrl}/login?logout=true");
    }

    #region Private Methods

    /// <summary>
    /// Extract the real client IP address, respecting proxy/CDN headers.
    /// </summary>
    private string GetClientIpAddress()
    {
        var cfIp = Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(cfIp))
        {
            return cfIp;
        }

        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var first = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(first))
            {
                return first;
            }
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Truncate User-Agent string to the column max length (256).
    /// </summary>
    private static string? TruncateDevice(string? userAgent)
    {
        return string.IsNullOrEmpty(userAgent) ? null
            : userAgent.Length <= 256 ? userAgent
            : userAgent[..256];
    }

    /// <summary>
    /// Validate post_logout_redirect_uri against registered PostLogoutRedirectUris for the client
    /// </summary>
    private async Task<bool> ValidatePostLogoutRedirectUriAsync(string? clientId, string postLogoutRedirectUri)
    {
        // Strip query params from the URI for matching
        // This allows /login?logout=true to match the registered /login
        var uriWithoutQuery = postLogoutRedirectUri;
        var queryIndex = postLogoutRedirectUri.IndexOf('?');
        if (queryIndex > 0)
        {
            uriWithoutQuery = postLogoutRedirectUri.Substring(0, queryIndex);
        }

        // If client_id provided, validate against that specific client
        if (!string.IsNullOrEmpty(clientId))
        {
            var app = await _applicationRepository.GetByClientIdAsync(clientId);
            if (app == null || !app.IsActive)
            {
                return false;
            }

            var allowedUris = ParseUriList(app.PostLogoutRedirectUris);
            return allowedUris.Any(uri =>
                string.Equals(uri, postLogoutRedirectUri, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri, uriWithoutQuery, StringComparison.OrdinalIgnoreCase));
        }

        // If no client_id, check all active applications
        var applications = await _applicationRepository.GetAllAsync();
        foreach (var app in applications.Where(a => a.IsActive))
        {
            var allowedUris = ParseUriList(app.PostLogoutRedirectUris);
            if (allowedUris.Any(uri =>
                    string.Equals(uri, postLogoutRedirectUri, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(uri, uriWithoutQuery, StringComparison.OrdinalIgnoreCase)))
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

        return uriString.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    #endregion
}
