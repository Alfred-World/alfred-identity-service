using Alfred.Identity.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL.EntityConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("generate_uuid_v7()");

        // NOTE: Standard Identity schema behavior for indexes
        builder.HasIndex(x => x.NormalizedUserName).IsUnique().HasDatabaseName("UserNameIndex");
        builder.HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex");

        builder.Property(x => x.UserName).HasMaxLength(256);
        builder.Property(x => x.NormalizedUserName).HasMaxLength(256);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.NormalizedEmail).HasMaxLength(256);

        // Custom fields
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(150);
    }
}
