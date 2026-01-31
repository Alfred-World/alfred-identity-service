using Alfred.Identity.Domain.Common.Base;
using Alfred.Identity.Domain.Common.Interfaces;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a user identity, aligned with AspNetUsers schema
/// </summary>
public class User : BaseEntity, IHasCreationTime, IHasCreator, IHasModificationTime, IHasModifier, IHasDeletionTime,
    IHasDeleter
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

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    public void AddRole(Guid roleId, Guid? creatorId = null)
    {
        if (UserRoles.Any(ur => ur.RoleId == roleId))
        {
            return;
        }

        UserRoles.Add(UserRole.Create(Id, roleId, creatorId));
    }

    public void RemoveRole(Guid roleId)
    {
        var role = UserRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (role != null)
        {
            UserRoles.Remove(role);
        }
    }

    private User()
    {
    }

    public static User Create(string email, string? passwordHash, string fullName, bool emailConfirmed = false,
        Guid? createdById = null)
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
            CreatedAt = DateTime.UtcNow,
            CreatedById = createdById
        };
    }

    // ... (rest of methods)

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

    public bool HasPassword()
    {
        return !string.IsNullOrEmpty(PasswordHash);
    }
}
