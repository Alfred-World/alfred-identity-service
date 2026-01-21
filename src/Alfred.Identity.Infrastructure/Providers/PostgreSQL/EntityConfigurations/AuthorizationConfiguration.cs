using Alfred.Identity.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL.EntityConfigurations;

public class AuthorizationConfiguration : IEntityTypeConfiguration<Authorization>
{
    public void Configure(EntityTypeBuilder<Authorization> builder)
    {
        builder.ToTable("authorizations");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ApplicationId, x.Status, x.Subject, x.Type })
               .HasDatabaseName("IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type");

        builder.Property(x => x.Subject).HasMaxLength(200);
        builder.Property(x => x.Status).HasMaxLength(50);
        builder.Property(x => x.Type).HasMaxLength(100);
        builder.Property(x => x.ConcurrencyToken).HasMaxLength(50);

        builder.HasOne(x => x.Application)
            .WithMany()
            .HasForeignKey(x => x.ApplicationId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
