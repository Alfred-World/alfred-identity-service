namespace Alfred.Identity.Domain.Entities;

using Alfred.Identity.Domain.Common.Base;

/// <summary>
/// Represents a role, aligned with AspNetRoles schema
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public string? ConcurrencyStamp { get; private set; } = Guid.NewGuid().ToString();

    // Custom fields if any

    private Role() { }

    public static Role Create(string name)
    {
        return new Role
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
    }
}
