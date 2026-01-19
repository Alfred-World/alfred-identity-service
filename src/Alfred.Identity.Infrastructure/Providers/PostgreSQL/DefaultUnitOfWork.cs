using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Email;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL;

/// <summary>
/// Default implementation of Unit of Work pattern
/// Database-agnostic - works with any IDbContext implementation
/// </summary>
public class DefaultUnitOfWork : IUnitOfWork
{
    private readonly IDbContext _context;
    private IEmailTemplateRepository? _emailTemplates;

    public DefaultUnitOfWork(IDbContext context)
    {
        _context = context;
    }

    public IEmailTemplateRepository EmailTemplates =>
        _emailTemplates ??= new EmailTemplateRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_context is DbContext dbContext)
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }

        throw new InvalidOperationException("DbContext is not available");
    }

    public void Dispose()
    {
        if (_context is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
