using System.Text.Json.Serialization;

using Alfred.Identity.Application;
using Alfred.Identity.Infrastructure;
using Alfred.Identity.WebApi.Configuration;
using Alfred.Identity.WebApi.Extensions;
using Alfred.Identity.WebApi.Middleware;

using FluentValidation;

using Microsoft.AspNetCore.HttpOverrides;

// Load environment variables from .env file
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
DotEnvLoader.LoadForEnvironment(environment);

// Load and validate configuration
AppConfiguration appConfig = new();
MtlsConfiguration mtlsConfig = new();

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with optional mTLS support
builder.ConfigureKestrelWithMtls(appConfig, mtlsConfig);

// Register configurations as singletons
builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton(mtlsConfig);

// Add Controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
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

// Add Swagger
builder.Services.AddSwaggerConfiguration();

// Add CORS
builder.Services.AddCorsConfiguration(appConfig);

// Add Application layer
builder.Services.AddApplication();

// Add HttpContextAccessor (required for ICurrentUser in Infrastructure layer)
builder.Services.AddHttpContextAccessor();

// Add Infrastructure layer (Database)
builder.Services.AddInfrastructure();

// Add Cookie Authentication for SSO
builder.Services.AddCookieAuthentication();

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

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
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Use Swagger in development
app.UseSwaggerInDevelopment();

// Add global exception handler (must be early in pipeline)
app.UseExceptionHandler();

app.UseCors("AllowFrontend");

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
