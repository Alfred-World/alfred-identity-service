using System.Reflection;

using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Options;
using Alfred.Identity.Infrastructure.Common.Seeding;
using Alfred.Identity.Infrastructure.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL;

/// <summary>
/// Extension methods for configuring PostgreSQL provider
/// Uses convention-based auto-registration to minimize boilerplate
/// </summary>
public static class ServiceCollectionExtensions
{
    private static readonly Assembly DomainAssembly = typeof(IUserRepository).Assembly;
    private static readonly Assembly InfraAssembly = typeof(ServiceCollectionExtensions).Assembly;

    public static IServiceCollection AddPostgreSQL(this IServiceCollection services, string connectionString)
    {
        PostgreSqlOptions options = new() { ConnectionString = connectionString };
        return AddPostgreSQL(services, options);
    }

    public static IServiceCollection AddPostgreSQL(this IServiceCollection services, PostgreSqlOptions options)
    {
        // === DbContext & Unit of Work ===
        services.AddScoped<IDbContextFactory, PostgreSqlDbContextFactory>(_ => new PostgreSqlDbContextFactory(options));
        services.AddScoped<IDbContext>(sp => sp.GetRequiredService<IDbContextFactory>().CreateContext());
        services.AddScoped(_ => new PostgreSqlDbContext(options));
        services.AddScoped<IUnitOfWork, DefaultUnitOfWork>();

        // === Auto-register Repositories (IXxxRepository -> XxxRepository) ===
        services.AddByConvention(
            interfaceAssembly: DomainAssembly,
            implementationAssembly: InfraAssembly,
            interfaceNamespace: "Alfred.Identity.Domain.Abstractions.Repositories",
            implementationNamespace: "Alfred.Identity.Infrastructure.Repositories"
        );

        // === Auto-register Services (IXxxService -> XxxService) ===
        services.AddByConvention(
            interfaceAssembly: DomainAssembly,
            implementationAssembly: InfraAssembly,
            interfaceNamespace: "Alfred.Identity.Domain.Abstractions.Security",
            implementationNamespace: "Alfred.Identity.Infrastructure.Services.Security"
        );
        
        // Services in Domain.Abstractions.Services -> Infrastructure.Services
        services.AddByConvention(
            interfaceAssembly: DomainAssembly,
            implementationAssembly: InfraAssembly,
            interfaceNamespace: "Alfred.Identity.Domain.Abstractions.Services",
            implementationNamespace: "Alfred.Identity.Infrastructure.Services"
        );

        // === Manual Registrations (namespace mismatch) ===
        // JwksService is in Services.Security but implements IJwksService from Domain.Abstractions.Services
        services.AddScoped<Domain.Abstractions.Services.IJwksService, Services.Security.JwksService>();

        // === HttpClient Services (special registration) ===
        services.AddHttpClient<ILocationService, IpApiLocationService>();

        // === Auto-register Data Seeders ===
        services.AddImplementationsOf<IDataSeeder>(InfraAssembly);

        return services;
    }

    /// <summary>
    /// Auto-register implementations matching interface naming convention (IXxx -> Xxx)
    /// </summary>
    private static void AddByConvention(
        this IServiceCollection services,
        Assembly interfaceAssembly,
        Assembly implementationAssembly,
        string interfaceNamespace,
        string implementationNamespace)
    {
        var interfaces = interfaceAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == interfaceNamespace);

        foreach (var iface in interfaces)
        {
            // Convention: IUserRepository -> UserRepository
            var implName = iface.Name.StartsWith("I") ? iface.Name[1..] : iface.Name;
            var implType = implementationAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == implName && t.Namespace == implementationNamespace && iface.IsAssignableFrom(t));

            if (implType != null)
            {
                services.AddScoped(iface, implType);
            }
        }
    }

    /// <summary>
    /// Auto-register all implementations of a given interface type
    /// </summary>
    private static void AddImplementationsOf<TInterface>(this IServiceCollection services, Assembly assembly)
    {
        var implementations = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(TInterface).IsAssignableFrom(t));

        foreach (var impl in implementations)
        {
            services.AddScoped(typeof(TInterface), impl);
        }
    }
}


