using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Roles.Common;

/// <summary>
/// DTO for Role entity with nullable properties for partial projection
/// </summary>
public class RoleDto
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? NormalizedName { get; set; }
    public bool? IsImmutable { get; set; }
    public bool? IsSystem { get; set; }
    public string? Icon { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public RoleDto()
    {
    }

    public static RoleDto FromEntity(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            NormalizedName = role.NormalizedName,
            IsImmutable = role.IsImmutable,
            IsSystem = role.IsSystem,
            Icon = role.Icon,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }
}
