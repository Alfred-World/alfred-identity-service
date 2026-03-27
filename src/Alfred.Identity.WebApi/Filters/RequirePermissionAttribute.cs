using System.Security.Claims;

using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.WebApi.Contracts.Common;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Alfred.Identity.WebApi.Filters;

/// <summary>
/// Requires at least one current user role to have the specified permission.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission?.Trim().ToLowerInvariant()
                      ?? throw new ArgumentNullException(nameof(permission));
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        if (!currentUser.IsAuthenticated || currentUser.UserId == null)
        {
            context.Result = new UnauthorizedObjectResult(ApiErrorResponse.Unauthorized("User not identified"));
            return;
        }

        var permissionCache = context.HttpContext.RequestServices.GetRequiredService<IPermissionCacheService>();
        var cancellationToken = context.HttpContext.RequestAborted;

        var rolesFromClaims = context.HttpContext.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Concat(context.HttpContext.User.FindAll("role").Select(c => c.Value))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var roles = new List<string>(rolesFromClaims);

        // Access tokens currently may not carry role claims; resolve roles from DB as fallback.
        if (!roles.Any())
        {
            var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            var user = await userRepository.GetByIdWithRolesAsync((UserId)currentUser.UserId.Value, cancellationToken);

            if (user != null)
            {
                roles = user.UserRoles
                    .Select(ur => ur.Role?.Name)
                    .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()!;
            }
        }

        if (!roles.Any())
        {
            context.Result = new ObjectResult(ApiErrorResponse.Forbidden("Access denied"))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        foreach (var role in roles)
        {
            if (await permissionCache.HasPermissionAsync(role, _permission, cancellationToken))
            {
                await next();
                return;
            }
        }

        context.Result = new ObjectResult(
            ApiErrorResponse.Forbidden("Access denied"))
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
