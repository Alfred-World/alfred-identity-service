using System.Linq.Expressions;

using Alfred.Identity.Domain.Abstractions.Email;
using Alfred.Identity.Domain.EmailTemplates;
using Alfred.Identity.Infrastructure.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for EmailTemplate
/// Database-agnostic implementation
/// </summary>
public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly IDbContext _context;
    private DbSet<EmailTemplate> DbSet => ((DbContext)_context).Set<EmailTemplate>();

    public EmailTemplateRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<EmailTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<EmailTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(e => e.Code == code.ToUpperInvariant(), cancellationToken);
    }

    public async Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(e => e.Name == name, cancellationToken);
    }

    public async Task<EmailTemplate?> GetByCategoryAsync(EmailTemplateCategory category,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(e => e.Category == category && e.IsActive && !e.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailTemplate>> GetActiveTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.IsActive && !e.IsDeleted)
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsCodeExistsAsync(string code, Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        code = code.ToUpperInvariant();
        var query = DbSet.Where(e => e.Code == code && !e.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => !e.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EmailTemplate>> FindAsync(
        Expression<Func<EmailTemplate, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(predicate)
            .Where(e => !e.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EmailTemplate entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(EmailTemplate entity)
    {
        DbSet.Update(entity);
    }

    public void Delete(EmailTemplate entity)
    {
        DbSet.Remove(entity);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(e => !e.IsDeleted, cancellationToken);
    }
}
