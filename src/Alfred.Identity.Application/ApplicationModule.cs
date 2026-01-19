using Alfred.Identity.Application.Querying.Parsing;

using Microsoft.Extensions.DependencyInjection;

namespace Alfred.Identity.Application;

/// <summary>
/// Application layer service configuration
/// </summary>
public static class ApplicationModule
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(typeof(ApplicationModule).Assembly); });

        // Register querying services
        services.AddScoped<IFilterParser, PrattFilterParser>();

        return services;
    }
}
