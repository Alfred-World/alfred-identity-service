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

        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("IX_OpenIddictScopes_Name");

        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.ConcurrencyToken).HasMaxLength(50);
    }
}
