using Alfred.Identity.Application;
using Alfred.Identity.Infrastructure;
using Alfred.Identity.Infrastructure.Common.HealthChecks;
using Alfred.Identity.Infrastructure.Common.Seeding;
using Alfred.Identity.Infrastructure.Providers.PostgreSQL;
using Alfred.Identity.WebApi.Configuration;
using Alfred.Identity.WebApi.Middleware;

using FluentValidation;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

// Load environment variables from .env file
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
DotEnvLoader.LoadForEnvironment(environment);

// Load and validate configuration
AppConfiguration appConfig = new();

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on the specified hostname and port from environment
builder.WebHost.ConfigureKestrel((context, options) => { options.ListenAnyIP(appConfig.AppPort); });

// Register AppConfiguration as singleton
builder.Services.AddSingleton(appConfig);

// Register AuthTokenService for Token Exchange Pattern
builder.Services.AddSingleton<Alfred.Identity.WebApi.Services.IAuthTokenService, Alfred.Identity.WebApi.Services.InMemoryAuthTokenService>();

// Add services to the container
builder.Services.AddControllers(options =>
    {
        // Add validation filter to handle FluentValidation errors in our standard format
        options.Filters.Add<ValidationFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Disable automatic 400 responses for model validation errors
        // Our ValidationFilter will handle it instead
        options.SuppressModelStateInvalidFilter = true;
    });

// Add FluentValidation - manual validation (no auto-validation to control error format)
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    // Don't map status codes to ProblemDetails - we handle it ourselves
    options.CustomizeProblemDetails = context => { context.ProblemDetails.Extensions.Clear(); };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HSE Management API",
        Version = "v1",
        Description = "API for Health, Safety, and Environment Management System"
    });

    // Enable annotations
    c.EnableAnnotations();

    // Support non-nullable reference types for proper required field detection in .NET 9
    c.SupportNonNullableReferenceTypes();

    c.UseAllOfForInheritance();
    c.UseAllOfToExtendReferenceSchemas();

    // Add JWT authentication to Swagger (for future use)
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
});

// Add CORS
builder.Services.AddCors(options =>
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

// Add Application layer
builder.Services.AddApplication();

// Add Infrastructure layer (Database)
builder.Services.AddInfrastructure();

// Add Cookie Authentication for SSO
var ssoCookieDomain = Environment.GetEnvironmentVariable("SSO_COOKIE_DOMAIN");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
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

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Validate all services before starting the application
await ValidateServicesAsync(app.Services);

// Log application startup info
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Environment: {Environment}", appConfig.Environment);
logger.LogInformation("Listening on: http://{Hostname}:{Port}", appConfig.AppHostname, appConfig.AppPort);

// Run database migrations automatically in production
if (app.Environment.IsProduction())
{
    logger.LogInformation("Running database migrations...");
    await RunDatabaseMigrationsAsync(app.Services, logger);

    // Run data seeders (environment-aware)
    logger.LogInformation("Running data seeders...");
    await RunDataSeedersAsync(app.Services, logger);
}

// Configure the HTTP request pipeline

// Add ForwardedHeaders middleware FIRST to properly handle X-Forwarded-* headers from YARP/Caddy
// This ensures cookies are set with the correct host (gateway.test instead of localhost)
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All,
    ForwardLimit = null, // No limit on forwards
};
// Clear default known networks/proxies to trust all (for development)
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HSE Management API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    // Always enable swagger endpoint for gateway access (even in production for documentation)
    app.UseSwagger();
}

// Add global exception handler (must be early in pipeline)
app.UseExceptionHandler();

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map Health Check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();

/// <summary>
/// Run database migrations automatically
/// </summary>
static async Task RunDatabaseMigrationsAsync(IServiceProvider services, ILogger<Program> logger)
{
    try
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgreSqlDbContext>();
        var migrations = await context.Database.GetPendingMigrationsAsync();

        if (migrations.Any())
        {
            logger.LogInformation("Found {MigrationCount} pending migration(s)", migrations.Count());
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations completed successfully");
        }
        else
        {
            logger.LogInformation("No pending migrations found");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error running database migrations");
        throw;
    }
}

/// <summary>
/// Run data seeders (environment-aware - runs different seeders based on environment)
/// </summary>
static async Task RunDataSeedersAsync(IServiceProvider services, ILogger<Program> logger)
{
    try
    {
        using var scope = services.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<DataSeederOrchestrator>();
        await orchestrator.SeedAllAsync();
        logger.LogInformation("Data seeding completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error running data seeders");
        throw;
    }
}

/// <summary>
/// Validate all infrastructure services are available before starting the application
/// </summary>
static async Task ValidateServicesAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var healthCheckOrchestrator =
        scope.ServiceProvider.GetRequiredService<HealthCheckOrchestrator>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var allHealthy = await healthCheckOrchestrator.ValidateAllServicesAsync();

    if (!allHealthy)
    {
        logger.LogCritical("[FATAL] Application startup failed - required services are unavailable");
        logger.LogCritical("Please check your configuration and ensure all services are running.");
        Environment.Exit(1);
    }
}

/// <summary>
/// Partial class to expose Program for integration tests
/// </summary>
public partial class Program
{
}
