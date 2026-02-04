using Alfred.Identity.Domain.Common.Base;
using Alfred.Identity.Domain.Common.Interfaces;

namespace Alfred.Identity.Domain.Entities;

public class UserBan : BaseEntity, IHasCreationTime, IHasCreator
{
    public Guid UserId { get; private set; }
    public string Reason { get; private set; } = null!;
    public DateTime BannedAt { get; private set; }
    public Guid? BannedById { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    
    // To track active status of usage
    public bool IsActive { get; private set; }
    
    public DateTime? UnbannedAt { get; private set; }
    public Guid? UnbannedById { get; private set; }

    // Audit interface implementation
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }

    // Navigation
    public virtual User User { get; private set; } = null!;

    private UserBan() { }

    public static UserBan Create(
        Guid userId, 
        string reason, 
        Guid? bannedById, 
        DateTime? expiresAt = null)
    {
        return new UserBan
        {
            UserId = userId,
            Reason = reason,
            BannedAt = DateTime.UtcNow,
            BannedById = bannedById,
            ExpiresAt = expiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedById = bannedById
        };
    }

    public void Unban(Guid? unbannedById)
    {
        if (!IsActive) return;

        IsActive = false;
        UnbannedAt = DateTime.UtcNow;
        UnbannedById = unbannedById;
    }
}
