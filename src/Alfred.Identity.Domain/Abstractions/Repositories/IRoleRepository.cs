using System.Linq.Expressions;

using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);
    Task DeleteAsync(Role role, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Build a paged query for roles with filtering, sorting, pagination.
    /// Returns IQueryable for handler to apply projection.
    /// </summary>
    new Task<(IQueryable<Role> Query, long Total)> BuildPagedQueryAsync(
        Expression<Func<Role, bool>>? filter,
        string? sort,
        int page,
        int pageSize,
        Expression<Func<Role, object>>[]? includes,
        Func<string, (Expression<Func<Role, object>>? Expression, bool CanSort)>? fieldSelector,
        CancellationToken cancellationToken = default);
}
