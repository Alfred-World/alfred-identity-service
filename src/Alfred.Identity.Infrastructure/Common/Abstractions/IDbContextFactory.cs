namespace Alfred.Identity.Infrastructure.Common.Abstractions;

/// <summary>
/// Factory for creating DbContext instances based on database provider
/// Enables easy switching between different database providers
/// </summary>
public interface IDbContextFactory
{
    IDbContext CreateContext();
}
