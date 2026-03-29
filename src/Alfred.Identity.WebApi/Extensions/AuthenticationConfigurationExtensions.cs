using System.Text.Json;

using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.WebApi.Configuration;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Alfred.Identity.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring Authentication
/// </summary>
public static class AuthenticationConfigurationExtensions
{
    // ── JWKS cache (same pattern as Gateway) ───────────────────────────────
    private static IList<JsonWebKey>? _cachedKeys;
    private static DateTime _keysLastFetched = DateTime.MinValue;
    private static readonly TimeSpan KeysCacheDuration = TimeSpan.FromHours(1);

    // Resolved after app.Build() to avoid BuildServiceProvider() on every cache miss
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Must be called once after <c>var app = builder.Build()</c> so the key resolver
    /// can use the real DI container instead of building a new one on each cache refresh.
    /// </summary>
    public static void RegisterServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Add Cookie + JWT Bearer dual authentication for the Identity Service.
    /// <para>
    ///   • Cookie auth is used by the SSO login flow (browser-based).
    ///   • JWT Bearer auth is used by API clients coming through the Gateway
    ///     (the Gateway already validates the JWT but the downstream service
    ///     still needs to parse the token to populate <c>HttpContext.User</c>
    ///     so that <c>ICurrentUser</c> works correctly).
    /// </para>
    /// </summary>
    public static IServiceCollection AddAuthenticationSchemes(
        this IServiceCollection services,
        AppConfiguration config)
    {
        services.AddAuthentication(options =>
            {
                // Use a policy scheme that picks the right handler per request
                options.DefaultScheme = "SmartScheme";
                options.DefaultChallengeScheme = "SmartScheme";
            })
            // ── Cookie scheme (SSO browser flow) ───────────────────────────
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "AlfredSession";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                    WriteAuthErrorAsync(
                        context.Response,
                        StatusCodes.Status401Unauthorized,
                        "You are not authorized to access this resource. Please provide a valid token.",
                        "UNAUTHORIZED");
                options.Events.OnRedirectToAccessDenied = context =>
                    WriteAuthErrorAsync(
                        context.Response,
                        StatusCodes.Status403Forbidden,
                        "You don't have permission to access this resource.",
                        "FORBIDDEN");
            })
            // ── JWT Bearer scheme (API calls via Gateway) ──────────────────
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = config.JwtIssuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,

                    // Resolve signing keys from our own DB (via ISigningKeyRepository)
                    IssuerSigningKeyResolver = (_, _, _, _) => GetSigningKeys()
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        if (context.Response.HasStarted)
                        {
                            return;
                        }

                        context.HandleResponse();
                        await WriteAuthErrorAsync(
                            context.Response,
                            StatusCodes.Status401Unauthorized,
                            "You are not authorized to access this resource. Please provide a valid token.",
                            "UNAUTHORIZED");
                    },
                    OnForbidden = context =>
                        WriteAuthErrorAsync(
                            context.Response,
                            StatusCodes.Status403Forbidden,
                            "You don't have permission to access this resource.",
                            "FORBIDDEN")
                };
            })
            // ── Policy scheme that auto-selects Cookie vs Bearer ───────────
            .AddPolicyScheme("SmartScheme", "Cookie or JWT", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

                    return authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
                        ? JwtBearerDefaults.AuthenticationScheme
                        : CookieAuthenticationDefaults.AuthenticationScheme;
                };
            });

        // Add Google Authentication if provided
        var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

        if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
        {
            // Need to get the auth builder again to chain Google
            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    options.SaveTokens = true;
                });
        }

        return services;
    }

    /// <summary>
    /// Load signing keys from database via <see cref="ISigningKeyRepository"/>.
    /// Keys are cached for 1 hour. Uses the registered <see cref="_serviceProvider"/> to
    /// avoid the expensive <c>BuildServiceProvider()</c> call on every cache refresh.
    /// </summary>
    private static IEnumerable<SecurityKey> GetSigningKeys()
    {
        if (_cachedKeys != null && DateTime.UtcNow - _keysLastFetched < KeysCacheDuration)
        {
            return _cachedKeys;
        }

        if (_serviceProvider is null)
        {
            // Called before RegisterServiceProvider — return cached keys or empty.
            // Token validation will fail (fail-closed), not pass.
            return _cachedKeys ?? Enumerable.Empty<SecurityKey>();
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var keyRepo = scope.ServiceProvider.GetRequiredService<ISigningKeyRepository>();

            var keys = keyRepo.GetValidKeysAsync(CancellationToken.None)
                .GetAwaiter().GetResult();

            var jwkList = new List<JsonWebKey>();

            foreach (var key in keys)
            {
                try
                {
                    var jwk = new JsonWebKey(key.PublicKey);
                    jwkList.Add(jwk);
                }
                catch
                {
                    // Skip malformed key entries
                }
            }

            _cachedKeys = jwkList;
            _keysLastFetched = DateTime.UtcNow;

            return _cachedKeys;
        }
        catch
        {
            // On refresh failure, continue serving cached keys if available
            return _cachedKeys ?? Enumerable.Empty<SecurityKey>();
        }
    }

    private static Task WriteAuthErrorAsync(HttpResponse response, int statusCode, string message, string code)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(new
        {
            success = false,
            errors = new[]
            {
                new
                {
                    message,
                    code
                }
            }
        });

        return response.WriteAsync(payload);
    }
}
