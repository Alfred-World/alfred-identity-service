using Alfred.Identity.Application.Querying.Filtering;

using FluentAssertions;

using Xunit;

namespace Alfred.Identity.Application.Tests.Querying.Filtering;

public class FilterSanitizerTests
{
    [Theory]
    [InlineData("name == 'John'")]
    [InlineData("status == 1 and isActive == true")]
    [InlineData("createdAt >= '2024-01-01'")]
    [InlineData("name @contains('test')")]
    [InlineData("(status == 1 or status == 2) and isActive == true")]
    public void Sanitize_ValidFilter_ReturnsFilter(string filter)
    {
        // Act
        var result = FilterSanitizer.Sanitize(filter);

        // Assert
        result.Should().Be(filter);
    }

    [Fact]
    public void Sanitize_NullOrEmpty_ReturnsEmpty()
    {
        FilterSanitizer.Sanitize(null).Should().BeEmpty();
        FilterSanitizer.Sanitize("").Should().BeEmpty();
        FilterSanitizer.Sanitize("   ").Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_ExceedsMaxLength_ThrowsException()
    {
        // Arrange
        var longFilter = new string('a', FilterSanitizer.MaxFilterLength + 1);

        // Act & Assert
        var act = () => FilterSanitizer.Sanitize(longFilter);
        act.Should().Throw<FilterSecurityException>()
            .Where(e => e.ViolationType == FilterSecurityViolationType.LengthExceeded);
    }

    [Theory]
    [InlineData("name == 'test'; DROP TABLE users'")]  // SQL injection
    [InlineData("name == 'test' -- comment")]          // SQL comment
    [InlineData("name == 'test' /* block */")]         // Block comment
    [InlineData("name == 'exec(cmd)'")]                // Exec attempt
    [InlineData("name == 'select * from users'")]      // Select statement
    [InlineData("name == 'union select 1'")]           // Union injection
    public void Sanitize_DangerousPattern_ThrowsException(string filter)
    {
        // Act & Assert
        var act = () => FilterSanitizer.Sanitize(filter);
        act.Should().Throw<FilterSecurityException>()
            .Where(e => e.ViolationType == FilterSecurityViolationType.DangerousPattern);
    }

    [Theory]
    [InlineData("name == 0x414243")]                   // Hex encoding (detected as dangerous pattern due to 0x)
    [InlineData("name == '0x48656C6C6F'")]             // Hex in string (detected as dangerous pattern due to 0x)
    public void Sanitize_HexEncoding_ThrowsException(string filter)
    {
        // Act & Assert - hex patterns are caught (either as dangerous pattern or hex encoding)
        var act = () => FilterSanitizer.Sanitize(filter);
        act.Should().Throw<FilterSecurityException>();
    }

    [Theory]
    [InlineData("name == ''''")]                       // Multiple quotes
    [InlineData("name == '''test'''")]                 // Triple quotes
    public void Sanitize_MultipleQuotes_ThrowsException(string filter)
    {
        // Act & Assert
        var act = () => FilterSanitizer.Sanitize(filter);
        act.Should().Throw<FilterSecurityException>()
            .Where(e => e.ViolationType == FilterSecurityViolationType.QuoteEscaping);
    }

    [Theory]
    [InlineData("name == 'test' and (status == 1")]    // Unbalanced open
    [InlineData("name == 'test' and status == 1)")]    // Unbalanced close
    [InlineData("((name == 'test')")]                  // Missing close
    public void Sanitize_UnbalancedParentheses_ThrowsException(string filter)
    {
        // Act & Assert
        var act = () => FilterSanitizer.Sanitize(filter);
        act.Should().Throw<FilterSecurityException>()
            .Where(e => e.ViolationType == FilterSecurityViolationType.UnbalancedParentheses);
    }

    [Fact]
    public void Sanitize_UnterminatedString_ThrowsException()
    {
        // Arrange
        var filter = "name == 'test";

        // Act & Assert
        var act = () => FilterSanitizer.Sanitize(filter);
        act.Should().Throw<FilterSecurityException>()
            .Where(e => e.ViolationType == FilterSecurityViolationType.UnterminatedString);
    }

    [Fact]
    public void Sanitize_StringLiteralTooLong_ThrowsException()
    {
        // Arrange
        var longString = new string('a', FilterSanitizer.MaxStringLiteralLength + 1);
        var filter = $"name == '{longString}'";

        // Act & Assert
        var act = () => FilterSanitizer.Sanitize(filter);
        act.Should().Throw<FilterSecurityException>()
            .Where(e => e.ViolationType == FilterSecurityViolationType.StringLiteralTooLong);
    }

    [Theory]
    [InlineData("test; DROP TABLE")]
    [InlineData("' OR 1=1 --")]
    [InlineData("exec(xp_cmdshell)")]
    [InlineData("0x48656C6C6F")]
    public void IsSuspiciousValue_DangerousValue_ReturnsTrue(string value)
    {
        // Act
        var result = FilterSanitizer.IsSuspiciousValue(value);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("John Doe")]
    [InlineData("test@example.com")]
    [InlineData("Hello World")]
    [InlineData("123-456-7890")]
    [InlineData(null)]
    [InlineData("")]
    public void IsSuspiciousValue_SafeValue_ReturnsFalse(string? value)
    {
        // Act
        var result = FilterSanitizer.IsSuspiciousValue(value);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("name @contains('\\u0041')")]          // Unicode escape
    public void Sanitize_UnicodeEscape_ThrowsException(string filter)
    {
        // Act & Assert
        var act = () => FilterSanitizer.Sanitize(filter);
        act.Should().Throw<FilterSecurityException>()
            .Where(e => e.ViolationType == FilterSecurityViolationType.UnicodeEscape);
    }
}
