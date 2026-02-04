using System.Reflection;

using Microsoft.OpenApi.Models;

namespace Alfred.Identity.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring Swagger/OpenAPI
/// </summary>
public static class SwaggerConfigurationExtensions
{
    /// <summary>
    /// Add Swagger/OpenAPI configuration
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
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

            // Add JWT authentication to Swagger
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
    /// Use Swagger middleware in development
    /// </summary>
    public static WebApplication UseSwaggerInDevelopment(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service v1");
                c.RoutePrefix = "swagger";
            });
        }

        return app;
    }
}
