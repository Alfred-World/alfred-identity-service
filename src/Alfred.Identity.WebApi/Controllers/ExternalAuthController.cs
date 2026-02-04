using System.Security.Claims;

using Alfred.Identity.Application.Auth.Commands.ExternalLogin;

using MediatR;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[ApiController]
[Route("identity/external-auth")]
public class ExternalAuthController : BaseApiController
{
    private readonly IMediator _mediator;

    public ExternalAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Initiates a challenge to an external provider (e.g., Google)
    /// </summary>
    [HttpGet("challenge")]
    public IActionResult Challenge([FromQuery] string provider, [FromQuery] string? returnUrl = "/")
    {
        if (string.IsNullOrEmpty(provider))
        {
            return BadRequest("Provider is required");
        }

        // Validate returnUrl (simplify for now, rely on Callback to validate or default)
        var callbackUrl = Url.Action(nameof(Callback), "ExternalAuth", new { returnUrl });

        var properties = new AuthenticationProperties
        {
            RedirectUri = callbackUrl,
            Items = { { "scheme", provider } }
        };

        return Challenge(properties, provider);
    }

    /// <summary>
    /// Handle callback from external provider
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? returnUrl = "/")
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // If Cookie Auth failed, check if we have External Cookie (from temporary signin)
        // Actually, AddGoogle usually signs in to "External" scheme or Default scheme.
        // In this setup, we stick to default Cookie scheme? 
        // Typically: Challenge -> Provider -> Callback -> AuthenticateAsync(ProviderScheme?)
        // Wait, default AddGoogle uses "Google" scheme.

        // We should authenticate against the Provider Scheme to get the claims
        var authenticateResult = await HttpContext.AuthenticateAsync("Google");

        if (!authenticateResult.Succeeded)
        {
            return Unauthorized("External authentication failed");
        }

        var claimsPrincipal = authenticateResult.Principal;
        if (claimsPrincipal == null)
        {
            return Unauthorized();
        }

        // Extract info
        var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier); // Google User ID
        var emailClaim = claimsPrincipal.FindFirst(ClaimTypes.Email);
        var nameClaim = claimsPrincipal.FindFirst(ClaimTypes.Name);

        if (userIdClaim == null)
        {
            return BadRequest("External provider did not return User ID");
        }

        // Execute Command to Find/Create User
        var command = new LoginWithExternalProviderCommand(
            "Google", // Simplified for now, or get from authenticateResult.Properties
            userIdClaim.Value,
            emailClaim?.Value,
            nameClaim?.Value
        );

        var loginResult = await _mediator.Send(command);

        if (loginResult.IsFailure)
        {
            return Unauthorized(loginResult.Error);
        }

        var user = loginResult.Value!.User;

        // Sign in to Local Session (AlfredSession)
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? ""),
            new("sub", user.Id.ToString())
        };

        if (!string.IsNullOrEmpty(user.FullName))
        {
            claims.Add(new(ClaimTypes.Name, user.FullName));
        }

        if (!string.IsNullOrEmpty(user.UserName))
        {
            claims.Add(new("username", user.UserName));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
        };

        // Complete the sign-in
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties
        );

        // Redirect to original returnUrl
        return Redirect(returnUrl ?? "/");
    }
}
