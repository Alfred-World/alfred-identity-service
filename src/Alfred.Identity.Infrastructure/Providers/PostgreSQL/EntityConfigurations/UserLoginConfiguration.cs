using Alfred.Identity.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL.EntityConfigurations;

public class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin>
{
    public void Configure(EntityTypeBuilder<UserLogin> builder)
    {
        builder.ToTable("user_logins");

        // PK is Id (inherited)
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("generate_uuid_v7()");

        // Composite Unique Index
        builder.HasIndex(x => new { x.LoginProvider, x.ProviderKey }).IsUnique();

        builder.Property(x => x.LoginProvider).HasMaxLength(128);

        builder.Property(x => x.ProviderKey).HasMaxLength(128);
        builder.Property(x => x.ProviderDisplayName).HasMaxLength(256);

        builder.HasOne(x => x.User)
            .WithMany(u => u.UserLogins)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
