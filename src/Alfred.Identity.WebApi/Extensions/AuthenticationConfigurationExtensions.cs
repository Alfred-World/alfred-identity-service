using Microsoft.AspNetCore.Authentication.Cookies;

namespace Alfred.Identity.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring Authentication
/// </summary>
public static class AuthenticationConfigurationExtensions
{
    /// <summary>
    /// Add Cookie Authentication for SSO
    /// </summary>
    public static IServiceCollection AddCookieAuthentication(this IServiceCollection services)
    {
        var authBuilder = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "AlfredSession";
                // Cookie domain is NOT set explicitly because:
                // 1. Request comes to localhost (via YARP reverse proxy)
                // 2. ASP.NET refuses to set cookie for different domain than request host
                // Instead, we rely on ForwardedHeaders middleware to detect the correct host
                // and the cookie will be set for that host (gateway.test when behind YARP)
                // 
                // For cross-subdomain sharing in production (e.g., *.alfred.com),
                // configure ForwardedHeaders properly and consider using cookie path/domain options
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.None; // Allow cross-origin cookie setting
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Required for SameSite=None
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                // For API-based auth, return 401 instead of redirect
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            });

        // Add Google Authentication if provided
        var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

        if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.SaveTokens = true;
            });
        }

        return services;
    }

}
