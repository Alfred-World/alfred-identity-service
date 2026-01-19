using System.Reflection;

using Alfred.Identity.Domain.EmailTemplates;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Options;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL;

/// <summary>
/// PostgreSQL DbContext for EF Core
/// Implements IDbContext to support provider switching
/// </summary>
public class PostgreSqlDbContext : DbContext, IDbContext
{
    private readonly PostgreSqlOptions _options;

    public PostgreSqlDbContext(PostgreSqlOptions options)
    {
        _options = options;
    }

    // System entities
    public DbSet<EmailTemplate> EmailTemplates { get; set; } = null!;

    // Add your Identity DbSets here as you develop
    // Example:
    // public DbSet<User> Users { get; set; }
    // public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_options.ConnectionString, npgsqlOptions =>
        {
            // store migrations history table
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
        });

        if (_options.EnableDetailedErrors)
        {
            optionsBuilder.EnableDetailedErrors();
        }

        if (_options.EnableSensitiveDataLogging)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-load all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
