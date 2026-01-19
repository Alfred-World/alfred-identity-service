using Alfred.Identity.Domain.Abstractions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Uncomment when Redis is needed
// using StackExchange.Redis;

namespace Alfred.Identity.Infrastructure.Providers.Cache;

/// <summary>
/// Extension methods for registering cache providers
/// </summary>
public static class CacheProviderExtensions
{
    /// <summary>
    /// Add cache provider services based on configuration
    /// Currently only InMemory is supported. Redis support can be enabled later.
    /// Can be configured via appsettings.json "Cache" section or environment variables:
    /// - CACHE_PROVIDER: "InMemory" (default), "Redis" (TODO)
    /// </summary>
    public static IServiceCollection AddCacheProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration with environment variable overrides
        services.Configure<CacheProviderOptions>(options =>
        {
            // Bind from appsettings first
            configuration.GetSection(CacheProviderOptions.SectionName).Bind(options);

            // Override with environment variables if present
            var envProvider = Environment.GetEnvironmentVariable("CACHE_PROVIDER");
            if (!string.IsNullOrEmpty(envProvider))
            {
                options.Provider = envProvider;
            }

            // TODO: Uncomment when Redis is needed
            // Redis settings
            // var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST");
            // if (!string.IsNullOrEmpty(redisHost))
            //     options.Redis.Host = redisHost;
        });

        // Register cache provider
        services.AddSingleton<ICacheProvider>(sp =>
        {
            var providerName = Environment.GetEnvironmentVariable("CACHE_PROVIDER") ?? "InMemory";

            // Currently only InMemory is supported
            switch (providerName.ToLowerInvariant())
            {
                // TODO: Uncomment when Redis is needed and add StackExchange.Redis package
                // case "redis":
                //     return CreateRedisCacheProvider(sp, configuration);

                case "inmemory":
                case "memory":
                default:
                    var logger = sp.GetRequiredService<ILogger<InMemoryCacheProvider>>();
                    logger.LogInformation("Using in-memory cache provider");
                    return new InMemoryCacheProvider(logger);
            }
        });

        return services;
    }

    // TODO: Uncomment when Redis is needed
    // private static ICacheProvider CreateRedisCacheProvider(
    //     IServiceProvider sp,
    //     IConfiguration configuration)
    // {
    //     var logger = sp.GetRequiredService<ILogger<RedisCacheProvider>>();
    //
    //     try
    //     {
    //         var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
    //         var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
    //         var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
    //
    //         var configOptions = new ConfigurationOptions
    //         {
    //             EndPoints = { $"{redisHost}:{redisPort}" },
    //             AbortOnConnectFail = false,
    //             ConnectRetry = 3,
    //             ConnectTimeout = 5000
    //         };
    //
    //         if (!string.IsNullOrEmpty(redisPassword))
    //             configOptions.Password = redisPassword;
    //
    //         var connection = ConnectionMultiplexer.Connect(configOptions);
    //         return new RedisCacheProvider(connection, logger);
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.LogWarning(ex, "Failed to connect to Redis. Falling back to in-memory cache.");
    //         var memLogger = sp.GetRequiredService<ILogger<InMemoryCacheProvider>>();
    //         return new InMemoryCacheProvider(memLogger);
    //     }
    // }

    /// <summary>
    /// Add in-memory cache provider explicitly
    /// </summary>
    public static IServiceCollection AddInMemoryCache(this IServiceCollection services)
    {
        services.AddSingleton<ICacheProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<InMemoryCacheProvider>>();
            logger.LogInformation("Using in-memory cache provider");
            return new InMemoryCacheProvider(logger);
        });

        return services;
    }
}
