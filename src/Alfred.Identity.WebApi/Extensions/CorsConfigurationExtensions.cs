using Alfred.Identity.WebApi.Configuration;

namespace Alfred.Identity.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring CORS
/// </summary>
public static class CorsConfigurationExtensions
{
    /// <summary>
    /// Add CORS policy based on AppConfiguration
    /// </summary>
    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        AppConfiguration appConfig)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                if (appConfig.CorsAllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(appConfig.CorsAllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        return services;
    }
}
