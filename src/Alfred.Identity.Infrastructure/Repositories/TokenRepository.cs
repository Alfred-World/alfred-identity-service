using System.Linq.Expressions;

using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Providers.PostgreSQL;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

public class TokenRepository : ITokenRepository
{
    private readonly PostgreSqlDbContext _context;

    public TokenRepository(PostgreSqlDbContext context)
    {
        _context = context;
    }

    public async Task<Token?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Tokens.FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<Token>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tokens.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Token>> FindAsync(Expression<Func<Token, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tokens.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Token entity, CancellationToken cancellationToken = default)
    {
        await _context.Tokens.AddAsync(entity, cancellationToken);
    }

    public void Update(Token entity)
    {
        _context.Tokens.Update(entity);
    }

    public void Delete(Token entity)
    {
        _context.Tokens.Remove(entity);
    }

    public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Tokens.AnyAsync(t => t.Id == id, cancellationToken);
    }

    public IQueryable<Token> GetQueryable()
    {
        return _context.Tokens.AsQueryable();
    }

    public async Task<Token?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        return await _context.Tokens.FirstOrDefaultAsync(t => t.ReferenceId == referenceId, cancellationToken);
    }

    public async Task<Token?> GetByAuthorizationIdAsync(long authorizationId, string type,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tokens.FirstOrDefaultAsync(t => t.AuthorizationId == authorizationId && t.Type == type,
            cancellationToken);
    }

    public async Task RevokeAllByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.Tokens
            .Where(t => t.UserId == userId && t.Status == "Valid")
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke();
        }
    }

    public async Task RevokeAllByAuthorizationIdAsync(long authorizationId,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _context.Tokens
            .Where(t => t.AuthorizationId == authorizationId && t.Status == "Valid")
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke();
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
