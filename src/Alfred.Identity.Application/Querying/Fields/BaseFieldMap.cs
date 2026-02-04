using System.Linq.Expressions;

namespace Alfred.Identity.Application.Querying.Fields;

/// <summary>
/// Base class for field maps - defines the contract for entity field configuration.
/// Provides access to field mappings for filtering, sorting, and projection.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public abstract class BaseFieldMap<TEntity> where TEntity : class
{
    /// <summary>
    /// Field map instance for filtering, sorting, and querying
    /// </summary>
    public abstract FieldMap<TEntity> Fields { get; }
}
