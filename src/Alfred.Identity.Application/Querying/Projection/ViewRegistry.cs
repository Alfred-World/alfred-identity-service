using System.Linq.Expressions;

using Alfred.Identity.Application.Querying.Fields;

namespace Alfred.Identity.Application.Querying.Projection;

/// <summary>
/// Registry of available views for an entity/DTO pair
/// Provides fluent API to register and lookup views
/// </summary>
/// <typeparam name="TEntity">Source entity type</typeparam>
/// <typeparam name="TDto">Target DTO type</typeparam>
public sealed class ViewRegistry<TEntity, TDto>
    where TEntity : class
    where TDto : class, new()
{
    private readonly Dictionary<string, ViewDefinition<TEntity, TDto>> _views = new(StringComparer.OrdinalIgnoreCase);
    private string? _defaultViewName;

    /// <summary>
    /// Register a view with its fields
    /// </summary>
    public ViewRegistry<TEntity, TDto> Register(
        string name,
        string[] fields,
        Expression<Func<TEntity, object>>[]? includes = null)
    {
        _views[name] = new ViewDefinition<TEntity, TDto>(name, fields, includes);
        return this;
    }

    /// <summary>
    /// Register a view with its fields using strongly-typed expressions
    /// </summary>
    public ViewRegistry<TEntity, TDto> Register(
        string name,
        Expression<Func<TDto, object?>>[] fields,
        Expression<Func<TEntity, object>>[]? includes = null)
    {
        var fieldNames = FieldExpressionHelper.GetFieldNames(fields);
        return Register(name, fieldNames, includes);
    }

    /// <summary>
    /// Set the default view to use when no view is specified
    /// </summary>
    public ViewRegistry<TEntity, TDto> SetDefault(string viewName)
    {
        if (!_views.ContainsKey(viewName))
        {
            throw new InvalidOperationException($"View '{viewName}' not found. Register it first.");
        }

        _defaultViewName = viewName;
        return this;
    }

    /// <summary>
    /// Get a view by name, or the default view if name is null/empty
    /// </summary>
    public ViewDefinition<TEntity, TDto> GetView(string? viewName)
    {
        if (string.IsNullOrWhiteSpace(viewName))
        {
            return GetDefaultView();
        }

        if (_views.TryGetValue(viewName, out var view))
        {
            return view;
        }

        throw new InvalidOperationException(
            $"View '{viewName}' not found. Available views: {string.Join(", ", _views.Keys)}");
    }

    /// <summary>
    /// Get the default view
    /// </summary>
    public ViewDefinition<TEntity, TDto> GetDefaultView()
    {
        if (_defaultViewName == null)
        {
            throw new InvalidOperationException("No default view set. Call SetDefault() first.");
        }

        return _views[_defaultViewName];
    }

    /// <summary>
    /// Check if a view exists
    /// </summary>
    public bool HasView(string viewName)
    {
        return _views.ContainsKey(viewName);
    }

    /// <summary>
    /// Get all registered view names
    /// </summary>
    public IEnumerable<string> GetViewNames()
    {
        return _views.Keys;
    }
}
