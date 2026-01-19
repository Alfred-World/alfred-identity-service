using Alfred.Identity.Domain.Abstractions.Email;

namespace Alfred.Identity.Domain.Abstractions;

/// <summary>
/// Unit of Work pattern - manages transactions and repositories
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // System repositories
    IEmailTemplateRepository EmailTemplates { get; }

    // Add your Identity repositories here as you develop
    // Example:
    // IUserRepository Users { get; }
    // IRefreshTokenRepository RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
