using System.Text.RegularExpressions;

namespace Alfred.Identity.Application.Querying.Filtering;

/// <summary>
/// Input sanitizer for filter DSL to prevent injection attacks.
/// Provides multiple layers of protection against SQL injection and XSS.
/// </summary>
public static partial class FilterSanitizer
{
    /// <summary>
    /// Maximum allowed filter string length
    /// </summary>
    public const int MaxFilterLength = 2048;

    /// <summary>
    /// Maximum allowed string literal length within filter
    /// </summary>
    public const int MaxStringLiteralLength = 500;

    /// <summary>
    /// Dangerous SQL patterns that should never appear in filter values
    /// </summary>
    private static readonly string[] DangerousPatterns =
    [
        "--",           // SQL comment
        ";",            // SQL statement terminator
        "/*",           // Block comment start
        "*/",           // Block comment end
        "xp_",          // Extended stored procedures
        "sp_",          // System stored procedures
        "exec(",        // Execute
        "execute(",     // Execute
        "insert(",      // Insert statement
        "update(",      // Update statement
        "delete(",      // Delete statement
        "drop(",        // Drop statement
        "truncate(",    // Truncate statement
        "alter(",       // Alter statement
        "create(",      // Create statement
        "union(",       // Union (SQL injection)
        "union ",       // Union with space
        "select(",      // Select statement
        "select ",      // Select with space
        "0x",           // Hex encoding
        "char(",        // Character conversion
        "nchar(",       // Unicode character
        "varchar(",     // Varchar conversion
        "cast(",        // Type casting
        "convert(",     // Type conversion
        "waitfor",      // Time-based injection
        "benchmark(",   // MySQL benchmark
        "sleep(",       // Sleep function
        "pg_sleep(",    // PostgreSQL sleep
        "<script",      // XSS script tag
        "javascript:",  // JavaScript protocol
        "onerror",      // XSS event handler
        "onload",       // XSS event handler
    ];

    /// <summary>
    /// SQL keywords that are suspicious when appearing in values
    /// </summary>
    private static readonly HashSet<string> SqlKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "TRUNCATE",
        "ALTER", "CREATE", "EXEC", "EXECUTE", "UNION", "WHERE",
        "FROM", "INTO", "VALUES", "SET", "TABLE", "DATABASE",
        "GRANT", "REVOKE", "DECLARE", "CURSOR", "FETCH", "OPEN"
    };

    /// <summary>
    /// Regex to detect hex-encoded strings (potential obfuscation)
    /// </summary>
    [GeneratedRegex(@"0x[0-9a-fA-F]{2,}", RegexOptions.Compiled)]
    private static partial Regex HexEncodingPattern();

    /// <summary>
    /// Regex to detect multiple consecutive quotes (quote escaping attack)
    /// </summary>
    [GeneratedRegex(@"'{3,}|""{3,}", RegexOptions.Compiled)]
    private static partial Regex MultipleQuotesPattern();

    /// <summary>
    /// Regex to detect Unicode escape sequences
    /// </summary>
    [GeneratedRegex(@"\\u[0-9a-fA-F]{4}", RegexOptions.Compiled)]
    private static partial Regex UnicodeEscapePattern();

    /// <summary>
    /// Sanitize and validate filter input.
    /// Returns sanitized string or throws if input is malicious.
    /// </summary>
    public static string Sanitize(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return string.Empty;
        }

        // 1. Length check
        if (filter.Length > MaxFilterLength)
        {
            throw new FilterSecurityException(
                $"Filter exceeds maximum length ({MaxFilterLength} characters)",
                FilterSecurityViolationType.LengthExceeded);
        }

        // 2. Check for dangerous patterns
        var lowerFilter = filter.ToLowerInvariant();
        foreach (var pattern in DangerousPatterns)
        {
            if (lowerFilter.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                throw new FilterSecurityException(
                    $"Filter contains potentially dangerous pattern",
                    FilterSecurityViolationType.DangerousPattern);
            }
        }

        // 3. Check for hex encoding (obfuscation attempt)
        if (HexEncodingPattern().IsMatch(filter))
        {
            throw new FilterSecurityException(
                "Filter contains hex-encoded values which are not allowed",
                FilterSecurityViolationType.HexEncoding);
        }

        // 4. Check for multiple consecutive quotes
        if (MultipleQuotesPattern().IsMatch(filter))
        {
            throw new FilterSecurityException(
                "Filter contains suspicious quote patterns",
                FilterSecurityViolationType.QuoteEscaping);
        }

        // 5. Check for Unicode escape sequences
        if (UnicodeEscapePattern().IsMatch(filter))
        {
            throw new FilterSecurityException(
                "Filter contains Unicode escape sequences which are not allowed",
                FilterSecurityViolationType.UnicodeEscape);
        }

        // 6. Validate balanced parentheses
        if (!HasBalancedParentheses(filter))
        {
            throw new FilterSecurityException(
                "Filter has unbalanced parentheses",
                FilterSecurityViolationType.UnbalancedParentheses);
        }

        // 7. Check string literals for suspicious content
        ValidateStringLiterals(filter);

        return filter;
    }

    /// <summary>
    /// Check if parentheses are balanced
    /// </summary>
    private static bool HasBalancedParentheses(string input)
    {
        var count = 0;
        var inString = false;
        var stringChar = '\0';

        foreach (var ch in input)
        {
            if (!inString && (ch == '\'' || ch == '"'))
            {
                inString = true;
                stringChar = ch;
            }
            else if (inString && ch == stringChar)
            {
                inString = false;
            }
            else if (!inString)
            {
                if (ch == '(')
                {
                    count++;
                }
                else if (ch == ')')
                {
                    count--;
                }

                if (count < 0)
                {
                    return false;
                }
            }
        }

        return count == 0;
    }

    /// <summary>
    /// Validate string literals within the filter
    /// </summary>
    private static void ValidateStringLiterals(string filter)
    {
        var inString = false;
        var stringChar = '\0';
        var currentLiteral = new System.Text.StringBuilder();

        for (var i = 0; i < filter.Length; i++)
        {
            var ch = filter[i];

            if (!inString && (ch == '\'' || ch == '"'))
            {
                inString = true;
                stringChar = ch;
                currentLiteral.Clear();
            }
            else if (inString && ch == stringChar)
            {
                // End of string literal - validate it
                var literal = currentLiteral.ToString();

                if (literal.Length > MaxStringLiteralLength)
                {
                    throw new FilterSecurityException(
                        $"String literal exceeds maximum length ({MaxStringLiteralLength})",
                        FilterSecurityViolationType.StringLiteralTooLong);
                }

                // Check for SQL keywords in string literals
                var words = literal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var sqlKeywordCount = words.Count(w => SqlKeywords.Contains(w));

                if (sqlKeywordCount >= 3)
                {
                    throw new FilterSecurityException(
                        "String literal contains multiple SQL keywords",
                        FilterSecurityViolationType.SuspiciousKeywords);
                }

                inString = false;
            }
            else if (inString)
            {
                currentLiteral.Append(ch);
            }
        }

        if (inString)
        {
            throw new FilterSecurityException(
                "Unterminated string literal",
                FilterSecurityViolationType.UnterminatedString);
        }
    }

    /// <summary>
    /// Check if a value looks like an SQL injection attempt
    /// Returns true if suspicious
    /// </summary>
    public static bool IsSuspiciousValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var lower = value.ToLowerInvariant();

        // Check dangerous patterns
        foreach (var pattern in DangerousPatterns)
        {
            if (lower.Contains(pattern))
            {
                return true;
            }
        }

        // Check for hex encoding
        if (HexEncodingPattern().IsMatch(value))
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Types of security violations detected
/// </summary>
public enum FilterSecurityViolationType
{
    LengthExceeded,
    DangerousPattern,
    HexEncoding,
    QuoteEscaping,
    UnicodeEscape,
    UnbalancedParentheses,
    StringLiteralTooLong,
    SuspiciousKeywords,
    UnterminatedString
}

/// <summary>
/// Exception thrown when a security violation is detected in filter input
/// </summary>
public class FilterSecurityException : Exception
{
    public FilterSecurityViolationType ViolationType { get; }

    public FilterSecurityException(string message, FilterSecurityViolationType violationType)
        : base(message)
    {
        ViolationType = violationType;
    }
}
