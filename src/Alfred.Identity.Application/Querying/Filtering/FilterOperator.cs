namespace Alfred.Identity.Application.Querying.Filtering;

/// <summary>
/// Các toán tử hỗ trợ trong filter DSL
/// </summary>
public enum FilterOperator
{
    // Logic operators
    And,
    Or,
    Not,

    // Comparison operators
    Equal, // ==
    NotEqual, // !=
    GreaterThan, // >
    GreaterThanOrEqual, // >=
    LessThan, // <
    LessThanOrEqual, // <=

    // String operators
    Contains, // @contains
    NotContains, // @ncontains
    StartsWith, // @startswith
    EndsWith, // @endswith

    // Set operators
    In, // @in
    NotIn, // @nin

    // Range operators
    Between, // @between

    // Null operators
    IsNull, // @isnull
    NotNull, // @notnull

    // Array operators
    Any, // @any
    All, // @all
    ContainsAny // @containsAny
}
