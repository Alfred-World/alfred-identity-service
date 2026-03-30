using System.Text.Json.Serialization;

using Alfred.Identity.Application;
using Alfred.Identity.Infrastructure;
using Alfred.Identity.Infrastructure.Common.Options;
using Alfred.Identity.WebApi.Configuration;
using Alfred.Identity.WebApi.Extensions;
using Alfred.Identity.WebApi.Middleware;

using FluentValidation;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

using Serilog;
using Serilog.Events;

// Load environment variables from .env file
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
DotEnvLoader.LoadForEnvironment(environment);

// Load and validate configuration
AppConfiguration appConfig = new();
MtlsConfiguration mtlsConfig = new();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Configure Kestrel with optional mTLS support
builder.ConfigureKestrelWithMtls(appConfig, mtlsConfig);

// Register configurations as singletons
builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton(mtlsConfig);

// Register JwtSettings — consolidated from AppConfiguration, used by JwtTokenService + WellKnownController
builder.Services.AddSingleton(new JwtSettings
{
    Issuer = appConfig.JwtIssuer,
    Audience = appConfig.JwtAudience,
    AccessTokenLifetimeSeconds = appConfig.JwtAccessTokenLifetimeSeconds,
    RefreshTokenLifetimeSeconds = appConfig.JwtRefreshTokenLifetimeSeconds
});

// Add Controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new StronglyTypedIdJsonConverterFactory());
    });

// Add API Versioning
builder.Services.AddApiVersioningConfiguration();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context => { context.ProblemDetails.Extensions.Clear(); };
});

// Add Scalar API documentation
builder.Services.AddScalarConfiguration();

// Add CORS
builder.Services.AddCorsConfiguration(appConfig);

// Add Application layer
builder.Services.AddApplication();

// Add HttpContextAccessor (required for ICurrentUser in Infrastructure layer)
builder.Services.AddHttpContextAccessor();

// Add Infrastructure layer (Database)
builder.Services.AddInfrastructure();

// Add Cookie + JWT Bearer Authentication
builder.Services.AddAuthenticationSchemes(appConfig);

// Add Rate Limiting (defense-in-depth; gateway is primary enforcer)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"success\":false,\"errors\":[{\"message\":\"Too many requests. Please try again later.\",\"code\":\"RATE_LIMIT_EXCEEDED\"}]}",
            ct);
    };

    // Auth endpoints: tight limit per client IP
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // General API: generous global limit per user/IP
    options.AddFixedWindowLimiter("default", opt =>
    {
        opt.PermitLimit = 200;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Register the built service provider so the JWT signing key resolver can use
// the real DI container instead of calling BuildServiceProvider() on each cache refresh.
AuthenticationConfigurationExtensions.RegisterServiceProvider(app.Services);

// Validate all services before starting the application
await app.ValidateServicesAsync();

// Log application startup info
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Environment: {Environment}", appConfig.Environment);
logger.LogInformation("Listening on: http://{Hostname}:{Port}", appConfig.AppHostname, appConfig.AppPort);

// Run production startup tasks (migrations and seeders)
await app.RunProductionStartupTasksAsync();

// Configure the HTTP request pipeline

// Add ForwardedHeaders middleware FIRST to properly handle X-Forwarded-* headers from YARP/Caddy
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.All,
    ForwardLimit = null
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Security headers (defense-in-depth)
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }

    await next();
});

// Use Scalar API reference in development
app.UseScalarInDevelopment();

// Add global exception handler (must be early in pipeline)
app.UseExceptionHandler();
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, _, exception) =>
    {
        if (exception is not null || httpContext.Response.StatusCode >= 500)
        {
            return LogEventLevel.Error;
        }

        return httpContext.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
            ? LogEventLevel.Verbose
            : LogEventLevel.Information;
    };
});

app.UseCors("AllowFrontend");

// Rate limiting (applied after CORS so preflight requests are not rate-limited)
app.UseRateLimiter();

// Only use HTTPS redirection if mTLS is not enabled
if (!mtlsConfig.Enabled)
{
    app.UseHttpsRedirection();
}

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map Health Check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();

/// <summary>
/// Partial class to expose Program for integration tests
/// </summary>
public partial class Program
{
}
