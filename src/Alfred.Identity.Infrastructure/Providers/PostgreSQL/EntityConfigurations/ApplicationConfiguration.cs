using System.Text.Json;

using Alfred.Identity.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

        // Value converter to ensure delimited strings (space, comma) are saved as JSON arrays in jsonb columns
        var jsonArrayConverter = new ValueConverter<string?, string?>(
            v => ToJsonArray(v),
            v => v);

        // Important: OpenIddict stores permissions/urls typically as JSON or delimited string. 
        // Schema implies they can be long text.
        builder.Property(x => x.DisplayNames).HasColumnType("jsonb");
        builder.Property(x => x.Permissions).HasColumnType("jsonb").HasConversion(jsonArrayConverter);
        builder.Property(x => x.RedirectUris).HasColumnType("jsonb").HasConversion(jsonArrayConverter);
        builder.Property(x => x.PostLogoutRedirectUris).HasColumnType("jsonb").HasConversion(jsonArrayConverter);
        builder.Property(x => x.Requirements).HasColumnType("jsonb");
        builder.Property(x => x.Settings).HasColumnType("jsonb");
        builder.Property(x => x.JsonWebKeySet).HasColumnType("jsonb");
    }

    private static string? ToJsonArray(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim();

        // If it's already a JSON array, leave it as is
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            return trimmed;
        }

        // Split by common delimiters and serialize to JSON array
        var parts = trimmed.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        return JsonSerializer.Serialize(parts);
    }
}
