namespace Alfred.Identity.Domain.Querying;

/// <summary>
/// Known comparison operators for field-level filtering (HotChocolate-style).
/// </summary>
public static class ComparisonOperators
{
    // Equality
    public const string Eq = "eq";
    public const string Neq = "neq";

    // Numeric / Date comparison
    public const string Gt = "gt";
    public const string Gte = "gte";
    public const string Lt = "lt";
    public const string Lte = "lte";

    // Set membership
    public const string In = "in";
    public const string Nin = "nin";

    // String operations
    public const string Contains = "contains";
    public const string Ncontains = "ncontains";
    public const string StartsWith = "startsWith";
    public const string NstartsWith = "nstartsWith";
    public const string EndsWith = "endsWith";
    public const string NendsWith = "nendsWith";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Eq, Neq, Gt, Gte, Lt, Lte, In, Nin,
        Contains, Ncontains, StartsWith, NstartsWith, EndsWith, NendsWith
    };

    public static bool IsComparisonOperator(string op)
    {
        return All.Contains(op);
    }
}

/// <summary>
/// Known collection operators for navigation/collection property filtering.
/// </summary>
public static class CollectionOperators
{
    public const string Some = "some";
    public const string AllOp = "all";
    public const string NoneOp = "none";
    public const string Any = "any";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Some, AllOp, NoneOp, Any
    };

    public static bool IsCollectionOperator(string op)
    {
        return All.Contains(op);
    }

    public static CollectionOperator Parse(string op)
    {
        return op.ToLowerInvariant() switch
        {
            "some" => CollectionOperator.Some,
            "all" => CollectionOperator.All,
            "none" => CollectionOperator.None,
            "any" => CollectionOperator.Any,
            _ => throw new ArgumentException($"Unknown collection operator: {op}")
        };
    }
}

/// <summary>
/// Maps .NET types to their available comparison operators for frontend metadata.
/// </summary>
public static class OperatorsByType
{
    private static readonly IReadOnlyList<string> EqualityOps = [ComparisonOperators.Eq, ComparisonOperators.Neq];

    private static readonly IReadOnlyList<string> ComparableOps =
    [
        ComparisonOperators.Eq, ComparisonOperators.Neq,
        ComparisonOperators.Gt, ComparisonOperators.Gte,
        ComparisonOperators.Lt, ComparisonOperators.Lte,
        ComparisonOperators.In, ComparisonOperators.Nin
    ];

    private static readonly IReadOnlyList<string> StringOps =
    [
        ComparisonOperators.Eq, ComparisonOperators.Neq,
        ComparisonOperators.Contains, ComparisonOperators.Ncontains,
        ComparisonOperators.StartsWith, ComparisonOperators.NstartsWith,
        ComparisonOperators.EndsWith, ComparisonOperators.NendsWith,
        ComparisonOperators.In, ComparisonOperators.Nin
    ];

    private static readonly IReadOnlyList<string> BooleanOps = [ComparisonOperators.Eq, ComparisonOperators.Neq];

    private static readonly IReadOnlyList<string> CollectionOps =
    [
        CollectionOperators.Some, CollectionOperators.AllOp,
        CollectionOperators.NoneOp, CollectionOperators.Any
    ];

    private static readonly Dictionary<string, IReadOnlyList<string>> TypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["string"] = StringOps,
        ["int"] = ComparableOps,
        ["long"] = ComparableOps,
        ["float"] = ComparableOps,
        ["double"] = ComparableOps,
        ["decimal"] = ComparableOps,
        ["boolean"] = BooleanOps,
        ["dateTime"] = ComparableOps,
        ["dateOnly"] = ComparableOps,
        ["timeOnly"] = ComparableOps,
        ["guid"] = EqualityOps,
        ["enum"] = [ComparisonOperators.Eq, ComparisonOperators.Neq, ComparisonOperators.In, ComparisonOperators.Nin],
        ["collection"] = CollectionOps
    };

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> AllTypes => TypeMap;

    public static IReadOnlyList<string> GetOperators(Type type)
    {
        var typeName = MapTypeName(type);
        return TypeMap.GetValueOrDefault(typeName) ?? EqualityOps;
    }

    public static string MapTypeName(Type type)
    {
        // Unwrap Nullable<T>
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string))
        {
            return "string";
        }

        if (underlying == typeof(int))
        {
            return "int";
        }

        if (underlying == typeof(long))
        {
            return "long";
        }

        if (underlying == typeof(float))
        {
            return "float";
        }

        if (underlying == typeof(double))
        {
            return "double";
        }

        if (underlying == typeof(decimal))
        {
            return "decimal";
        }

        if (underlying == typeof(bool))
        {
            return "boolean";
        }

        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset))
        {
            return "dateTime";
        }

        if (underlying == typeof(DateOnly))
        {
            return "dateOnly";
        }

        if (underlying == typeof(TimeOnly))
        {
            return "timeOnly";
        }

        if (underlying == typeof(Guid))
        {
            return "guid";
        }

        if (underlying.IsEnum)
        {
            return "enum";
        }

        // Check if it's a collection (IEnumerable<T> but not string)
        if (underlying != typeof(string) && underlying.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        {
            return "collection";
        }

        return "object";
    }
}
