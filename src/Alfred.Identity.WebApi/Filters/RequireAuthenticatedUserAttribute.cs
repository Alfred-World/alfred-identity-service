using Alfred.Identity.Domain.Abstractions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Alfred.Identity.WebApi.Filters;

/// <summary>
/// Action filter that short-circuits with 401 when ICurrentUser.UserId is null.
/// Apply at controller level to replace repeated null-checks in every action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireAuthenticatedUserAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        if (currentUser.UserId == null)
        {
            context.Result = new UnauthorizedObjectResult(
                ApiErrorResponse.Unauthorized("User not identified"));
        }
    }
}
