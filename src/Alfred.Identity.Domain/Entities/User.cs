namespace Alfred.Identity.Domain.Entities;

using Alfred.Identity.Domain.Common.Base;

/// <summary>
/// Represents a user identity, aligned with AspNetUsers schema
/// </summary>
public class User : BaseEntity
{
    public string UserName { get; private set; } = null!;
    public string NormalizedUserName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;
    public bool EmailConfirmed { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? SecurityStamp { get; private set; }
    public string? ConcurrencyStamp { get; private set; } = Guid.NewGuid().ToString();
    public string? PhoneNumber { get; private set; }
    public bool PhoneNumberConfirmed { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }
    public bool LockoutEnabled { get; private set; }
    public int AccessFailedCount { get; private set; }
    
    // Custom fields not in standard Identity but useful
    public string FullName { get; private set; } = null!;
    public string Status { get; private set; } = "Active";

    private User() { }

    public static User Create(string email, string? passwordHash, string fullName, bool emailConfirmed = false)
    {
        return new User
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = emailConfirmed,
            PasswordHash = passwordHash,
            FullName = fullName,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
    }

    public void RecordLoginSuccess()
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
    }

    public void RecordLoginFailure()
    {
        AccessFailedCount++;
        // Simple lockout logic could go here
    }

    public void VerifyEmail()
    {
        EmailConfirmed = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanLogin()
    {
        return Status == "Active" && (!LockoutEnd.HasValue || LockoutEnd < DateTimeOffset.UtcNow);
    }

    public void SetUserName(string userName)
    {
        UserName = userName;
        NormalizedUserName = userName.ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }
    public bool HasPassword() => !string.IsNullOrEmpty(PasswordHash);
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}
