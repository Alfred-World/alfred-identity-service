using Alfred.Identity.Application.Applications;
using Alfred.Identity.Application.Common.Behaviors;
using Alfred.Identity.Application.Common.Events;
using Alfred.Identity.Application.Permissions;
using Alfred.Identity.Application.Querying.Filtering.Parsing;
using Alfred.Identity.Application.Roles;
using Alfred.Identity.Application.Users;
using Alfred.Identity.Domain.Common.Events;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace Alfred.Identity.Application;

/// <summary>
/// Application layer service configuration
/// </summary>
public static class ApplicationModule
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR (still used by Auth/Account/Connect/ExternalAuth/Keys controllers)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationModule).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Register Validators
        services.AddValidatorsFromAssembly(typeof(ApplicationModule).Assembly);

        // Register querying services
        services.AddScoped<IFilterParser, PrattFilterParser>();

        // Register domain services
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IPermissionService, PermissionService>();

        // Domain event dispatcher (Domain port -> Application adapter)
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        return services;
    }
}
