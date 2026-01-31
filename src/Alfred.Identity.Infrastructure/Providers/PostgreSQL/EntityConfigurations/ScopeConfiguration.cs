using Alfred.Identity.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL.EntityConfigurations;

public class ScopeConfiguration : IEntityTypeConfiguration<Scope>
{
    public void Configure(EntityTypeBuilder<Scope> builder)
    {
        builder.ToTable("scopes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("generate_uuid_v7()");

        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("IX_OpenIddictScopes_Name");

        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.ConcurrencyToken).HasMaxLength(50);

        builder.Property(x => x.DisplayNames).HasColumnType("jsonb");
        builder.Property(x => x.Descriptions).HasColumnType("jsonb");
        builder.Property(x => x.Resources).HasColumnType("jsonb");
        builder.Property(x => x.Properties).HasColumnType("jsonb");
    }
}
