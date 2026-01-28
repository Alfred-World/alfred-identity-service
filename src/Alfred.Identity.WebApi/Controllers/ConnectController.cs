using System.Security.Claims;

using Alfred.Identity.Application.Auth.Commands.Authorize;
using Alfred.Identity.Application.Auth.Commands.ExchangeCode;
using Alfred.Identity.WebApi.Contracts.Connect;

using MediatR;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[ApiController]
[Route("connect")]
public class ConnectController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public ConnectController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    /// <summary>
    /// OAuth2/OIDC Authorize Endpoint
    /// </summary>
    /// <remarks>
    /// Handles the interactive login flow. Validates the client, redirects to login if needed, 
    /// and issues an authorization code upon successful authentication and consent.
    /// </remarks>
    /// <param name="request">Authorization request parameters</param>
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken] // For Postman testing ease, but strictly should be secured
    public async Task<IActionResult> Authorize([FromQuery] AuthorizeRequest request)
    {
        // Check if User is Authenticated
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);


        if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
        {
            // If prompt=none, return error
            if (request.prompt == "none")
            {
                return BadRequest(new { error = "login_required" });
            }

            // Redirect to SSO Login Page
            // Build returnUrl - use X-Forwarded headers or config when behind Gateway proxy
            var gatewayUrl = _configuration["Urls:Gateway"] ?? "https://gateway.test";

            // Check for X-Forwarded headers (when behind proxy)
            var forwardedHost = Request.Headers["X-Forwarded-Host"].FirstOrDefault();
            var forwardedProto = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? "https";

            string returnUrl;
            if (!string.IsNullOrEmpty(forwardedHost))
            {
                returnUrl = $"{forwardedProto}://{forwardedHost}{Request.Path}{Request.QueryString}";
            }
            else
            {
                // Use Gateway URL from config
                returnUrl = $"{gatewayUrl}{Request.Path}{Request.QueryString}";
            }

            var ssoUrl = _configuration["Urls:SsoWeb"] ?? "https://sso.test";
            var loginUrl = $"{ssoUrl}/login?returnUrl={Uri.EscapeDataString(returnUrl)}";

            return Redirect(loginUrl);
        }

        // 2. Extract User ID
        var userIdClaim = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? authenticateResult.Principal.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return BadRequest(new { error = "invalid_user" });
        }

        // 3. Execute Authorize Command
        var command = new AuthorizeCommand(
            request.client_id,
            request.redirect_uri,
            request.response_type,
            request.scope,
            request.state,
            request.code_challenge,
            request.code_challenge_method,
            request.prompt,
            userId
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            // If error, return generic error or redirect with error param
            return BadRequest(new { error = result.Error, error_description = result.ErrorDescription });
        }

        // 4. Redirect Back to Client with Code
        return Redirect(result.RedirectLocation!);
    }

    /// <summary>
    /// OAuth2/OIDC Token Endpoint
    /// </summary>
    /// <remarks>
    /// Exchanges authorization code for access/ID tokens, or refreshes existing tokens.
    /// Supports 'authorization_code' and 'refresh_token' grant types.
    /// </remarks>
    /// <param name="request">Token exchange request parameters</param>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token([FromForm] ExchangeCodeRequest request)
    {
        var command = new ExchangeCodeCommand(
            request.grant_type,
            request.client_id,
            request.client_secret,
            request.code,
            request.redirect_uri,
            request.code_verifier,
            request.refresh_token
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error, error_description = result.ErrorDescription });
        }

        return Ok(new
        {
            access_token = result.AccessToken,
            refresh_token = result.RefreshToken,
            id_token = result.IdToken,
            token_type = result.TokenType,
            expires_in = result.ExpiresIn
        });
    }

    /// <summary>
    /// OIDC Logout / End Session Endpoint
    /// </summary>
    /// <remarks>
    /// Clears the user's single sign-on (SSO) session cookie.
    /// Can optionally redirect the user back to the client application after logout.
    /// </remarks>
    /// <param name="client_id">Client Identifier (optional)</param>
    /// <param name="post_logout_redirect_uri">URL to redirect to after logout (optional)</param>
    /// <param name="id_token_hint">ID Token hint (optional)</param>
    /// <param name="state">State parameter to pass back (optional)</param>
    [HttpGet("logout")]
    public async Task<IActionResult> Logout(
        [FromQuery] string? client_id,
        [FromQuery] string? post_logout_redirect_uri,
        [FromQuery] string? id_token_hint,
        [FromQuery] string? state)
    {
        // Sign out from cookie authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // If post_logout_redirect_uri is provided, redirect there
        if (!string.IsNullOrEmpty(post_logout_redirect_uri))
        {
            // TODO: Validate post_logout_redirect_uri against registered URIs for client_id
            // For now, just redirect
            var redirectUrl = post_logout_redirect_uri;
            if (!string.IsNullOrEmpty(state))
            {
                redirectUrl += (redirectUrl.Contains('?') ? "&" : "?") + $"state={state}";
            }

            return Redirect(redirectUrl);
        }

        // If no redirect URI, show a simple logged out message or redirect to SSO home
        var ssoUrl = _configuration["Urls:SsoWeb"] ?? "https://sso.test";
        return Redirect($"{ssoUrl}/login?logout=true");
    }
}
