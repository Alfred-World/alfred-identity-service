using Alfred.Identity.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL.EntityConfigurations;

public class TokenConfiguration : IEntityTypeConfiguration<Token>
{
    public void Configure(EntityTypeBuilder<Token> builder)
    {
        builder.ToTable("tokens");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ReferenceId).IsUnique().HasDatabaseName("IX_OpenIddictTokens_ReferenceId");
        builder.HasIndex(x => x.AuthorizationId).HasDatabaseName("IX_OpenIddictTokens_AuthorizationId");
        builder.HasIndex(x => new { x.ApplicationId, x.Status, x.Subject, x.Type })
            .HasDatabaseName("IX_OpenIddictTokens_ApplicationId_Status_Subject_Type");

        builder.Property(x => x.ReferenceId).HasMaxLength(100);
        builder.Property(x => x.Properties).HasColumnType("jsonb");
        builder.Property(x => x.Payload).HasColumnType("jsonb");

        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.Property(x => x.Location).HasMaxLength(256);
        builder.Property(x => x.Device).HasMaxLength(256);

        builder.Property(x => x.Status).HasMaxLength(50);
        builder.Property(x => x.Type).HasMaxLength(50);
        builder.Property(x => x.Subject).HasMaxLength(200);
        builder.Property(x => x.ConcurrencyToken).HasMaxLength(50);

        builder.HasOne(x => x.Application)
            .WithMany()
            .HasForeignKey(x => x.ApplicationId)
            .IsRequired(false);

        builder.HasOne(x => x.Authorization)
            .WithMany()
            .HasForeignKey(x => x.AuthorizationId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .IsRequired(false) // User is optional for some tokens (e.g. client credentials)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
