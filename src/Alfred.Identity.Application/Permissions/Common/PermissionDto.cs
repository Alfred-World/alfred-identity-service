using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Permissions.Common;

public record PermissionDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    string Resource,
    string Action,
    bool IsActive
)
{
    public static PermissionDto FromEntity(Permission permission)
    {
        return new PermissionDto(
            permission.Id,
            permission.Code,
            permission.Name,
            permission.Description,
            permission.Resource,
            permission.Action,
            permission.IsActive
        );
    }
}
