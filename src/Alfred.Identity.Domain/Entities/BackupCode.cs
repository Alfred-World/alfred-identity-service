using Alfred.Identity.Domain.Common.Base;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Backup code for 2FA recovery.
/// Each code can only be used once.
/// </summary>
public class BackupCode : BaseEntity
{
    /// <summary>
    /// Hash of the backup code value
    /// </summary>
    public string CodeHash { get; private set; } = null!;

    /// <summary>
    /// The user this code belongs to
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Whether this code has been used
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// When this code was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this code was used (null if not used)
    /// </summary>
    public DateTime? UsedAt { get; private set; }

    // Navigation property
    public virtual User User { get; private set; } = null!;

    // Private constructor for EF Core
    private BackupCode()
    {
    }

    /// <summary>
    /// Create a new backup code
    /// </summary>
    public static BackupCode Create(string codeHash, Guid userId)
    {
        return new BackupCode
        {
            CodeHash = codeHash,
            UserId = userId,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Mark this code as used
    /// </summary>
    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if this code is valid (not used)
    /// </summary>
    public bool IsValid()
    {
        return !IsUsed;
    }
}
