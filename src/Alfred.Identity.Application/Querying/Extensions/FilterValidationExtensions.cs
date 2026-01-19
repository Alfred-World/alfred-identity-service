using Alfred.Identity.Application.Querying.Parsing;
using Alfred.Identity.Application.Querying.Validation;

namespace Alfred.Identity.Application.Querying.Extensions;

/// <summary>
/// Extension methods để validate filter safely
/// </summary>
public static class FilterValidationExtensions
{
    /// <summary>
    /// Validate filter string an toàn (check length, parse, validate)
    /// Trả về error nếu có vấn đề
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) TryValidateFilter<T>(
        string? filterString,
        IFilterParser parser,
        FieldMap<T> fieldMap)
    {
        if (string.IsNullOrEmpty(filterString))
        {
            return (true, null); // No filter is valid
        }

        try
        {
            // Parse filter
            var filterNode = parser.Parse(filterString);

            // Validate filter
            FilterValidator.Validate(filterNode, fieldMap);

            return (true, null);
        }
        catch (ArgumentException ex)
        {
            return (false, $"Invalid filter: {ex.Message}");
        }
        catch (FormatException ex)
        {
            return (false, $"Filter syntax error: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return (false, $"Filter validation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Unexpected error processing filter: {ex.GetType().Name}");
        }
    }

    /// <summary>
    /// Validate filter và throw exception nếu invalid
    /// </summary>
    public static void ValidateFilterOrThrow<T>(
        string? filterString,
        IFilterParser parser,
        FieldMap<T> fieldMap)
    {
        if (string.IsNullOrEmpty(filterString))
        {
            return; // No filter is valid
        }

        var (isValid, errorMessage) = TryValidateFilter(filterString, parser, fieldMap);

        if (!isValid)
        {
            throw new ArgumentException(errorMessage ?? "Invalid filter");
        }
    }
}
