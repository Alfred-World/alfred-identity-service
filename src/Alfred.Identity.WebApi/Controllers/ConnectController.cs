using Alfred.Identity.Application.Auth.Commands.Authorize;
using Alfred.Identity.Application.Auth.Commands.ExchangeCode;
using Alfred.Identity.WebApi.Contracts.Connect;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken] // For Postman testing ease, but strictly should be secured
    public async Task<IActionResult> Authorize([FromQuery] AuthorizeRequest request)
    {
        // 1. Check if User is Authenticated
        // For API, we might rely on Cookie Auth which Identity Service should support for SSO.
        // Assuming Cookies.
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
            ClientId: request.client_id,
            RedirectUri: request.redirect_uri,
            ResponseType: request.response_type,
            Scope: request.scope,
            State: request.state,
            CodeChallenge: request.code_challenge,
            CodeChallengeMethod: request.code_challenge_method,
            Prompt: request.prompt,
            UserId: userId
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

    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token([FromForm] ExchangeCodeRequest request)
    {
        var command = new ExchangeCodeCommand(
            GrantType: request.grant_type,
            ClientId: request.client_id,
            ClientSecret: request.client_secret,
            Code: request.code,
            RedirectUri: request.redirect_uri,
            CodeVerifier: request.code_verifier,
            RefreshToken: request.refresh_token
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
    /// OIDC Logout / End Session endpoint
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

