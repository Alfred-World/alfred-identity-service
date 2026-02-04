using System.Reflection;

using Alfred.Identity.Domain.Entities;


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

    // Identity entities
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<BackupCode> BackupCodes { get; set; } = null!;
    public DbSet<UserBan> UserBans { get; set; } = null!;
    public DbSet<UserActivityLog> UserActivityLogs { get; set; } = null!;
    public DbSet<UserLogin> UserLogins { get; set; } = null!;



    // SSO
    public DbSet<Application> Applications { get; set; } = null!;
    public DbSet<Authorization> Authorizations { get; set; } = null!;
    public DbSet<Scope> Scopes { get; set; } = null!;
    public DbSet<Token> Tokens { get; set; } = null!; // General purpose tokens (AuthCode, Reset, etc)
    public DbSet<SigningKey> SigningKeys { get; set; } = null!; // JWKS keys
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_options.ConnectionString, npgsqlOptions =>
        {
            // store migrations history table
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history");
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
