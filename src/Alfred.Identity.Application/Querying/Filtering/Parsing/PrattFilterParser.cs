using System.Globalization;

using Alfred.Identity.Application.Querying.Filtering.Ast;

namespace Alfred.Identity.Application.Querying.Filtering.Parsing;

/// <summary>
/// Pratt parser cho filter DSL
/// Hỗ trợ: AND, OR, NOT, ==, !=, >, >=, <, <=, @contains, @startswith, @endswith, @in, @between, @isnull, @notnull, etc.
/// </summary>
public sealed class PrattFilterParser : IFilterParser
{
    private const int MaxFilterLength = 2048;
    private List<FilterToken> _tokens = new();
    private int _current;

    public FilterNode Parse(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            throw new ArgumentException("Filter cannot be empty", nameof(filter));
        }

        // Security: Sanitize input before parsing
        var sanitizedFilter = FilterSanitizer.Sanitize(filter);

        if (sanitizedFilter.Length > MaxFilterLength)
        {
            throw new ArgumentException(
                $"Filter string too long (max {MaxFilterLength} characters, got {sanitizedFilter.Length})",
                nameof(filter));
        }

        FilterTokenizer tokenizer = new(sanitizedFilter);
        _tokens = tokenizer.Tokenize().ToList();
        _current = 0;

        var result = ParseExpression(0);

        if (!IsAtEnd())
        {
            throw new FormatException($"Unexpected token '{CurrentToken.Value}' at position {CurrentToken.Position}");
        }

        return result;
    }

    private FilterNode ParseExpression(int precedence)
    {
        var left = ParsePrefix();

        while (!IsAtEnd() && precedence < GetPrecedence(CurrentToken))
        {
            left = ParseInfix(left);
        }

        return left;
    }

    private FilterNode ParsePrefix()
    {
        var token = CurrentToken;

        // Parentheses
        if (token.Type == FilterTokenType.LeftParen)
        {
            Advance();
            var expr = ParseExpression(0);
            Expect(FilterTokenType.RightParen, "Expected ')'");
            return new GroupNode(expr);
        }

        // NOT operator
        if (token.Type == FilterTokenType.Operator && token.Value.ToLower() == "not")
        {
            Advance();
            var operand = ParseExpression(GetPrecedence(token));
            return new UnaryNode(FilterOperator.Not, operand);
        }

        // Field or literal
        if (token.Type == FilterTokenType.Identifier)
        {
            Advance();
            return new FieldNode(token.Value);
        }

        if (token.Type == FilterTokenType.String)
        {
            Advance();
            return LiteralNode.String(token.Value);
        }

        if (token.Type == FilterTokenType.Number)
        {
            Advance();
            if (token.Value.Contains('.'))
            {
                return LiteralNode.Number(double.Parse(token.Value, CultureInfo.InvariantCulture));
            }
            else
            {
                return LiteralNode.Integer(long.Parse(token.Value, CultureInfo.InvariantCulture));
            }
        }

        if (token.Type == FilterTokenType.Boolean)
        {
            Advance();
            return LiteralNode.Boolean(bool.Parse(token.Value));
        }

        if (token.Type == FilterTokenType.Null)
        {
            Advance();
            return LiteralNode.Null();
        }

        throw new FormatException($"Unexpected token '{token.Value}' at position {token.Position}");
    }

    private FilterNode ParseInfix(FilterNode left)
    {
        var token = CurrentToken;

        if (token.Type != FilterTokenType.Operator)
        {
            throw new FormatException($"Expected operator, got '{token.Value}' at position {token.Position}");
        }

        var op = token.Value.ToLower();

        // Binary logical operators: and, or
        if (op is "and" or "or")
        {
            var filterOp = op == "and" ? FilterOperator.And : FilterOperator.Or;
            Advance();
            var right = ParseExpression(GetPrecedence(token));
            return new BinaryNode(filterOp, left, right);
        }

        // Binary comparison operators: ==, !=, >, >=, <, <=
        if (op is "==" or "!=" or ">" or ">=" or "<" or "<=")
        {
            var filterOp = op switch
            {
                "==" => FilterOperator.Equal,
                "!=" => FilterOperator.NotEqual,
                ">" => FilterOperator.GreaterThan,
                ">=" => FilterOperator.GreaterThanOrEqual,
                "<" => FilterOperator.LessThan,
                "<=" => FilterOperator.LessThanOrEqual,
                _ => throw new NotSupportedException()
            };
            Advance();
            var right = ParseExpression(GetPrecedence(token) + 1);
            return new BinaryNode(filterOp, left, right);
        }

        // Function-like operators: @contains, @startswith, @in, @between, etc.
        if (op.StartsWith("@"))
        {
            return ParseCallOperator(left, op);
        }

        throw new FormatException($"Unknown operator '{op}' at position {token.Position}");
    }

    private FilterNode ParseCallOperator(FilterNode target, string op)
    {
        Advance();

        var filterOp = op switch
        {
            "@contains" => FilterOperator.Contains,
            "@ncontains" => FilterOperator.NotContains,
            "@startswith" => FilterOperator.StartsWith,
            "@endswith" => FilterOperator.EndsWith,
            "@in" => FilterOperator.In,
            "@nin" => FilterOperator.NotIn,
            "@between" => FilterOperator.Between,
            "@isnull" => FilterOperator.IsNull,
            "@notnull" => FilterOperator.NotNull,
            "@any" => FilterOperator.Any,
            "@all" => FilterOperator.All,
            "@containsany" => FilterOperator.ContainsAny,
            _ => throw new FormatException($"Unknown operator '{op}'")
        };

        // Nullary operators: @isnull, @notnull - no arguments needed
        if (filterOp is FilterOperator.IsNull or FilterOperator.NotNull)
        {
            return new UnaryNode(filterOp, target);
        }

        // All other operators require parentheses (standard function call syntax)
        Expect(FilterTokenType.LeftParen, $"Expected '(' after {op}. Use {op}('value') for function call syntax.");

        List<FilterNode> arguments = new();

        // Parse comma-separated arguments
        if (CurrentToken.Type != FilterTokenType.RightParen)
        {
            do
            {
                arguments.Add(ParseExpression(0));

                if (CurrentToken.Type == FilterTokenType.Comma)
                {
                    Advance();
                }
                else
                {
                    break;
                }
            } while (CurrentToken.Type != FilterTokenType.RightParen);
        }

        Expect(FilterTokenType.RightParen, "Expected ')'");

        return new CallNode(filterOp, target, arguments);
    }

    private int GetPrecedence(FilterToken token)
    {
        if (token.Type != FilterTokenType.Operator)
        {
            return 0;
        }

        return token.Value.ToLower() switch
        {
            "or" => 1,
            "and" => 2,
            "not" => 3,
            "==" or "!=" => 4,
            ">" or ">=" or "<" or "<=" => 4,
            _ when token.Value.StartsWith("@") => 5,
            _ => 0
        };
    }

    private FilterToken CurrentToken => _tokens[_current];

    private bool IsAtEnd()
    {
        return _current >= _tokens.Count || CurrentToken.Type == FilterTokenType.EndOfInput;
    }

    private void Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }
    }

    private void Expect(FilterTokenType type, string message)
    {
        if (CurrentToken.Type != type)
        {
            throw new FormatException($"{message} at position {CurrentToken.Position}");
        }

        Advance();
    }
}
