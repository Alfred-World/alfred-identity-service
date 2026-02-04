using System.Linq.Expressions;

using Alfred.Identity.Application.Querying.Fields;

namespace Alfred.Identity.Application.Querying.Projection;

/// <summary>
/// Registry of available views for an entity/DTO pair.
/// Provides fluent API to register and lookup views with field alias support.
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
        Expression<Func<TEntity, object>>[]? includes = null,
        Dictionary<string, string>? fieldAliases = null)
    {
        _views[name] = new ViewDefinition<TEntity, TDto>(name, fields, includes, fieldAliases);
        return this;
    }

    /// <summary>
    /// Register a view with its fields using strongly-typed expressions
    /// </summary>
    public ViewRegistry<TEntity, TDto> Register(
        string name,
        Expression<Func<TDto, object?>>[] fields,
        Expression<Func<TEntity, object>>[]? includes = null,
        Dictionary<string, string>? fieldAliases = null)
    {
        var fieldNames = FieldExpressionHelper.GetFieldNames(fields);
        return Register(name, fieldNames, includes, fieldAliases);
    }

    /// <summary>
    /// Register a view with field aliases using fluent builder
    /// </summary>
    public ViewRegistry<TEntity, TDto> Register(
        string name,
        Action<ViewBuilder<TEntity, TDto>> configure)
    {
        var builder = new ViewBuilder<TEntity, TDto>(name);
        configure(builder);
        _views[name] = builder.Build();
        return this;
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

/// <summary>
/// Fluent builder for creating ViewDefinitions with field aliases
/// </summary>
public sealed class ViewBuilder<TEntity, TDto>
    where TEntity : class
    where TDto : class, new()
{
    private readonly string _name;
    private readonly List<string> _fields = new();
    private readonly Dictionary<string, string> _fieldAliases = new();
    private readonly List<Expression<Func<TEntity, object>>> _includes = new();

    public ViewBuilder(string name)
    {
        _name = name;
    }

    /// <summary>
    /// Add a simple field to the view
    /// </summary>
    public ViewBuilder<TEntity, TDto> Select(Expression<Func<TDto, object?>> field)
    {
        var fieldName = FieldExpressionHelper.GetFieldName(field);
        _fields.Add(fieldName);
        return this;
    }

    /// <summary>
    /// Add a field with an alias (DTO property -> FieldMap key).
    /// Example: SelectAs(r => r.Permissions, "permissionsSummary") 
    /// means DTO's Permissions property will be populated from FieldMap's "permissionsSummary" expression.
    /// </summary>
    public ViewBuilder<TEntity, TDto> SelectAs(Expression<Func<TDto, object?>> dtoField, string fieldMapKey)
    {
        var dtoFieldName = FieldExpressionHelper.GetFieldName(dtoField);
        _fields.Add(dtoFieldName);
        _fieldAliases[dtoFieldName] = fieldMapKey;
        return this;
    }

    /// <summary>
    /// Add an include for eager loading navigation properties
    /// </summary>
    public ViewBuilder<TEntity, TDto> Include(Expression<Func<TEntity, object>> include)
    {
        _includes.Add(include);
        return this;
    }

    internal ViewDefinition<TEntity, TDto> Build()
    {
        return new ViewDefinition<TEntity, TDto>(
            _name,
            _fields.ToArray(),
            _includes.Count > 0 ? _includes.ToArray() : null,
            _fieldAliases.Count > 0 ? _fieldAliases : null);
    }
}
