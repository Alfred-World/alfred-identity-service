using Alfred.Identity.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL.EntityConfigurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        // Composite primary key
        builder.HasKey(x => new { x.RoleId, x.PermissionId });

        // Index for querying permissions by role (most common query)
        builder.HasIndex(x => x.RoleId)
            .HasDatabaseName("IX_role_permissions_role_id");

        // Relationship: RolePermission -> Permission
        // Note: RolePermission -> Role is configured in RoleConfiguration
        builder.HasOne(x => x.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

