using System.Reflection;

using Microsoft.OpenApi.Models;

using Scalar.AspNetCore;

namespace Alfred.Identity.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring Scalar API documentation
/// </summary>
public static class ScalarConfigurationExtensions
{
    /// <summary>
    /// Add OpenAPI specification generation configuration
    /// </summary>
    public static IServiceCollection AddScalarConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Identity Service API",
                Version = "v1",
                Description = "API for Alfred Identity Management System"
            });

            // Enable annotations
            c.EnableAnnotations();

            // Support non-nullable reference types for proper required field detection in .NET 9
            c.SupportNonNullableReferenceTypes();

            c.UseAllOfForInheritance();
            c.UseAllOfToExtendReferenceSchemas();

            // Add JWT authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Enter only your JWT token (the Bearer prefix will be added automatically)",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new List<string>()
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    /// <summary>
    /// Use Scalar API reference in development
    /// </summary>
    public static WebApplication UseScalarInDevelopment(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.MapScalarApiReference("/docs", c =>
            {
                c.Title = "Identity Service API";
                c.Theme = ScalarTheme.Purple;
                c.DefaultHttpClient =
                    new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.HttpClient);
                c.OpenApiRoutePattern = "/swagger/{documentName}/swagger.json";
                c.PersistentAuthentication = true;
            });
        }

        return app;
    }
}
