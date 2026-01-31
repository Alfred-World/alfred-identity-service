using Alfred.Identity.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL.EntityConfigurations;

/// <summary>
/// EF Core configuration for BackupCode entity
/// </summary>
public class BackupCodeConfiguration : IEntityTypeConfiguration<BackupCode>
{
    public void Configure(EntityTypeBuilder<BackupCode> builder)
    {
        builder.ToTable("backup_codes");

        builder.HasKey(bc => bc.Id);

        builder.Property(bc => bc.Id)
            .HasDefaultValueSql("generate_uuid_v7()");

        builder.Property(bc => bc.CodeHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(bc => bc.UserId)
            .IsRequired();

        builder.Property(bc => bc.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(bc => bc.CreatedAt)
            .IsRequired();

        // Index for finding unused codes for a user
        builder.HasIndex(bc => new { bc.UserId, bc.IsUsed });
    }
}
