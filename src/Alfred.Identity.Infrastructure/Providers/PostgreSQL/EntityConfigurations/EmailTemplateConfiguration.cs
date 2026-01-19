using Alfred.Identity.Domain.EmailTemplates;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL.EntityConfigurations;

/// <summary>
/// EF Core entity configuration for EmailTemplate
/// </summary>
public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");

        // Primary Key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(EmailTemplate.MaxCodeLength);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(EmailTemplate.MaxNameLength);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(EmailTemplate.MaxSubjectLength);

        builder.Property(e => e.HtmlBody)
            .IsRequired();

        builder.Property(e => e.PlainTextBody)
            .IsRequired(false);

        builder.Property(e => e.Description)
            .HasMaxLength(EmailTemplate.MaxDescriptionLength)
            .IsRequired(false);

        builder.Property(e => e.AvailablePlaceholders)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.IsSystem)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.Category)
            .IsRequired()
            .HasConversion<int>();

        // Audit fields
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.CreatedById);

        builder.Property(e => e.UpdatedAt);

        builder.Property(e => e.UpdatedById);

        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt);

        builder.Property(e => e.DeletedById);

        // Indexes
        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(e => e.Name);

        builder.HasIndex(e => e.Category);

        builder.HasIndex(e => new { e.IsActive, e.IsDeleted });

        // Ignore domain events (not persisted)
        builder.Ignore(e => e.DomainEvents);
    }
}
