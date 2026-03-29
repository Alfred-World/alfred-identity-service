using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Permissions.Common;

/// <summary>
/// DTO for Permission entity.
/// All string fields are nullable to support partial view projection.
/// JSON serializer skips null values to reduce bandwidth.
/// </summary>
public sealed class PermissionDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Resource { get; set; }
    public string? Action { get; set; }
    public bool? IsActive { get; set; }

    public static PermissionDto FromEntity(Permission permission)
    {
        return new PermissionDto
        {
            Id = permission.Id.Value,
            Code = permission.Code,
            Name = permission.Name,
            Description = permission.Description,
            Resource = permission.Resource,
            Action = permission.Action,
            IsActive = permission.IsActive
        };
    }
}
