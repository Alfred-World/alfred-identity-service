using Alfred.Identity.Domain.Common.Base;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a scope, aligned with OpenIddictScopes schema
/// </summary>
public class Scope : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string? DisplayName { get; private set; }
    public string? DisplayNames { get; private set; } // JSON
    public string? Description { get; private set; }
    public string? Descriptions { get; private set; } // JSON
    public string? Resources { get; private set; } // JSON or Space delimited
    public string? Properties { get; private set; } // JSON
    public string? ConcurrencyToken { get; private set; } = Guid.NewGuid().ToString();

    public static Scope Create(string name, string? displayName = null, string? description = null,
        string? resources = null)
    {
        return new Scope
        {
            Name = name,
            DisplayName = displayName,
            Description = description,
            Resources = resources,
            ConcurrencyToken = Guid.NewGuid().ToString()
        };
    }
}
