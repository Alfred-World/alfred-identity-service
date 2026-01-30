using System.Linq.Expressions;

namespace Alfred.Identity.Application.Querying.Fields;

/// <summary>
/// FieldMap defines a whitelist of fields that can be queried/sorted and their types
/// </summary>
public sealed class FieldMap<T>
{
    private readonly Dictionary<string, FieldMapping> _map = new(StringComparer.OrdinalIgnoreCase);

    public FieldBuilder Add<TProp>(
        string name,
        Expression<Func<T, TProp>> selector)
    {
        var mapping = new FieldMapping(selector, typeof(TProp), false, false, false);
        _map[name] = mapping;
        return new FieldBuilder(this, name);
    }

    private void UpdateMapping(string name, Func<FieldMapping, FieldMapping> updateAction)
    {
        if (_map.TryGetValue(name, out var mapping))
        {
            _map[name] = updateAction(mapping);
        }
    }

    /// <summary>
    /// Builder class for fluent field configuration
    /// </summary>
    public class FieldBuilder
    {
        private readonly FieldMap<T> _map;
        private readonly string _name;

        public FieldBuilder(FieldMap<T> map, string name)
        {
            _map = map;
            _name = name;
        }

        public FieldBuilder Filterable()
        {
            _map.UpdateMapping(_name, m => m with { CanFilter = true });
            return this;
        }

        public FieldBuilder Sortable()
        {
            _map.UpdateMapping(_name, m => m with { CanSort = true });
            return this;
        }

        public FieldBuilder Selectable()
        {
            _map.UpdateMapping(_name, m => m with { CanSelect = true });
            return this;
        }

        public FieldMap<T> AllowAll()
        {
            _map.UpdateMapping(_name, m => m with { CanFilter = true, CanSort = true, CanSelect = true });
            return _map;
        }

        // Allow continuing the chain on the map itself
        public FieldBuilder Add<TProp>(string name, Expression<Func<T, TProp>> selector)
        {
            return _map.Add(name, selector);
        }

        public FieldMap<T> Build()
        {
            return _map;
        }

        // Implicit conversion to allow chaining back to FieldMap
        public static implicit operator FieldMap<T>(FieldBuilder builder)
        {
            return builder._map;
        }
    }

    public bool TryGet(string name, out LambdaExpression expression, out Type type)
    {
        if (_map.TryGetValue(name, out var mapping))
        {
            expression = mapping.Expression;
            type = mapping.Type;
            return true;
        }

        expression = null!;
        type = null!;
        return false;
    }

    public bool CanFilter(string name)
    {
        return _map.TryGetValue(name, out var mapping) && mapping.CanFilter;
    }

    public bool CanSort(string name)
    {
        return _map.TryGetValue(name, out var mapping) && mapping.CanSort;
    }

    public bool CanSelect(string name)
    {
        return _map.TryGetValue(name, out var mapping) && mapping.CanSelect;
    }

    public bool ContainsField(string name)
    {
        return _map.ContainsKey(name);
    }

    public IEnumerable<string> GetFieldNames()
    {
        return _map.Keys;
    }

    public IEnumerable<(string FieldName, LambdaExpression Expression, Type Type)> GetAllFields()
    {
        return _map.Select(kvp => (kvp.Key, kvp.Value.Expression, kvp.Value.Type));
    }

    private sealed record FieldMapping(
        LambdaExpression Expression,
        Type Type,
        bool CanFilter,
        bool CanSort,
        bool CanSelect);
}
