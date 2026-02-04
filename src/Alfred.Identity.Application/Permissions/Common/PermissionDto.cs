using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Permissions.Common;

/// <summary>
/// DTO for Permission entity.
/// Non-essential fields are nullable to support partial projection.
/// JSON serializer will skip null values to reduce bandwidth.
/// </summary>
public record PermissionDto(
    Guid Id,
    string Code,
    string Name,
    string? Description = null,
    string? Resource = null,
    string? Action = null,
    bool? IsActive = null
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
