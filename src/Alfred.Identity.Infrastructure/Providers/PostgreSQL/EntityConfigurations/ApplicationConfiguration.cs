using Alfred.Identity.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL.EntityConfigurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.ToTable("applications");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ClientId).IsUnique().HasDatabaseName("IX_OpenIddictApplications_ClientId");

        builder.Property(x => x.ClientId).HasMaxLength(100);
        builder.Property(x => x.ApplicationType).HasMaxLength(50);
        builder.Property(x => x.ClientType).HasMaxLength(50);
        builder.Property(x => x.ConsentType).HasMaxLength(50);
        builder.Property(x => x.ConcurrencyToken).HasMaxLength(50);
        
        // Important: OpenIddict stores permissions/urls typically as JSON or delimited string. 
        // Schema implies they can be long text.
    }
}
