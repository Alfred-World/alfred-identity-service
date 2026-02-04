using Alfred.Identity.Domain.Common.Base;
using Alfred.Identity.Domain.Common.Interfaces;

using Alfred.Identity.Domain.Enums;

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
    public string? Avatar { get; private set; }
    public bool PhoneNumberConfirmed { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }

    public bool LockoutEnabled { get; private set; }
    public int AccessFailedCount { get; private set; }

    // Custom fields not in standard Identity but useful
    public string FullName { get; private set; } = null!;
    public UserStatus Status { get; private set; } = UserStatus.Active;

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }

    // Banning
    public bool IsBanned { get; private set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
    public virtual ICollection<UserBan> UserBans { get; private set; } = new List<UserBan>();
    public virtual ICollection<UserActivityLog> ActivityLogs { get; private set; } = new List<UserActivityLog>();
    public virtual ICollection<UserLogin> UserLogins { get; private set; } = new List<UserLogin>();

    public void AddLogin(string loginProvider, string providerKey, string? displayName)
    {
        if (UserLogins.Any(x => x.LoginProvider == loginProvider && x.ProviderKey == providerKey))
        {
            return;
        }

        UserLogins.Add(UserLogin.Create(loginProvider, providerKey, displayName, Id));
    }

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


    public void Ban(string reason, Guid? bannedById, DateTime? expiresAt = null)
    {
        if (IsBanned)
        {
            return;
        }

        IsBanned = true;
        Status = UserStatus.Banned;
        LockoutEnabled = true;
        LockoutEnd = expiresAt ?? DateTimeOffset.MaxValue;
        UpdatedAt = DateTime.UtcNow;
        UpdatedById = bannedById;

        // Add ban record
        UserBans.Add(UserBan.Create(Id, reason, bannedById, expiresAt));
    }

    public void Unban(Guid? unbannedById)
    {
        if (!IsBanned)
        {
            return;
        }

        IsBanned = false;
        Status = UserStatus.Active;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
        UpdatedById = unbannedById;

        // Deactivate active ban
        var activeBan = UserBans.FirstOrDefault(b => b.IsActive);
        activeBan?.Unban(unbannedById);
    }

    public static User Create(string email, string? passwordHash, string fullName, bool emailConfirmed = false,
        Guid? createdById = null)
    {
        return CreateWithUsername(email, email, passwordHash, fullName, emailConfirmed, createdById);
    }

    public static User CreateWithUsername(string email, string userName, string? passwordHash, string fullName, bool emailConfirmed = false,
        Guid? createdById = null)
    {
        return new User
        {
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = emailConfirmed,
            PasswordHash = passwordHash,
            FullName = fullName,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedById = createdById
        };
    }


    // ...

    public bool CanLogin()
    {
        return !IsBanned && Status == UserStatus.Active && (!LockoutEnd.HasValue || LockoutEnd < DateTimeOffset.UtcNow);
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

    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnableTwoFactor()
    {
        // Require secret to be set before enabling
        if (string.IsNullOrEmpty(TwoFactorSecret))
        {
            throw new InvalidOperationException("Cannot enable 2FA without a secret key.");
        }
        TwoFactorEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTwoFactorSecret(string secret)
    {
        TwoFactorSecret = secret;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorSecret = null; // Clear secret on disable? Or keep it? Usually clear to force reset.
        UpdatedAt = DateTime.UtcNow;
    }


}

