using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Roles.Common;

public record RoleDto(
    long Id,
    string Name,
    bool IsImmutable,
    bool IsSystem,
    DateTime CreatedAt
)
{
    public static RoleDto FromEntity(Role role)
    {
        return new RoleDto(
            role.Id,
            role.Name,
            role.IsImmutable,
            role.IsSystem,
            role.CreatedAt
        );
    }
}
