using Alfred.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL.EntityConfigurations;

public class SigningKeyConfiguration : IEntityTypeConfiguration<SigningKey>
{
    public void Configure(EntityTypeBuilder<SigningKey> builder)
    {
        builder.ToTable("signing_keys");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.KeyId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Algorithm)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.PublicKey)
            .IsRequired(); // Text/JSON

        builder.Property(x => x.PrivateKey)
            .IsRequired(); // Text/JSON

        builder.HasIndex(x => x.KeyId)
            .IsUnique();
            
        builder.HasIndex(x => x.IsActive);
    }
}
