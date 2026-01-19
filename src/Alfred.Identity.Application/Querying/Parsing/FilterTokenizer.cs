using System.Text;

namespace Alfred.Identity.Application.Querying.Parsing;

/// <summary>
/// Tokenizer cho filter DSL
/// </summary>
public sealed class FilterTokenizer
{
    private readonly string _input;
    private int _position;

    public FilterTokenizer(string input)
    {
        _input = input ?? string.Empty;
        _position = 0;
    }

    public IEnumerable<FilterToken> Tokenize()
    {
        while (_position < _input.Length)
        {
            SkipWhitespace();

            if (_position >= _input.Length)
            {
                break;
            }

            var ch = _input[_position];

            // Operators và special characters
            if (ch == '(')
            {
                yield return FilterToken.LeftParen(_position++);
                continue;
            }

            if (ch == ')')
            {
                yield return FilterToken.RightParen(_position++);
                continue;
            }

            if (ch == ',')
            {
                yield return FilterToken.Comma(_position++);
                continue;
            }

            // String literals
            if (ch == '\'' || ch == '"')
            {
                yield return ReadString(ch);
                continue;
            }

            // Numbers
            if (char.IsDigit(ch) || (ch == '-' && _position + 1 < _input.Length && char.IsDigit(_input[_position + 1])))
            {
                yield return ReadNumber();
                continue;
            }

            // Operators: ==, !=, >=, <=, >, <, @xxx
            if (IsOperatorStart(ch))
            {
                yield return ReadOperator();
                continue;
            }

            // Identifiers và keywords
            if (char.IsLetter(ch) || ch == '_')
            {
                yield return ReadIdentifierOrKeyword();
                continue;
            }

            throw new FormatException($"Unexpected character '{ch}' at position {_position}");
        }

        yield return FilterToken.EndOfInput(_position);
    }

    private void SkipWhitespace()
    {
        while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
        {
            _position++;
        }
    }

    private FilterToken ReadString(char quote)
    {
        var start = _position;
        _position++; // Skip opening quote

        StringBuilder value = new();
        while (_position < _input.Length && _input[_position] != quote)
        {
            if (_input[_position] == '\\' && _position + 1 < _input.Length)
            {
                _position++;
                value.Append(_input[_position]);
            }
            else
            {
                value.Append(_input[_position]);
            }

            _position++;
        }

        if (_position >= _input.Length)
        {
            throw new FormatException($"Unterminated string at position {start}");
        }

        _position++; // Skip closing quote
        return FilterToken.String(value.ToString(), start);
    }

    private FilterToken ReadNumber()
    {
        var start = _position;
        if (_input[_position] == '-')
        {
            _position++;
        }

        while (_position < _input.Length && (char.IsDigit(_input[_position]) || _input[_position] == '.'))
        {
            _position++;
        }

        return FilterToken.Number(_input[start.._position], start);
    }

    private FilterToken ReadOperator()
    {
        var start = _position;

        // @ operators (@contains, @startswith, etc.)
        if (_input[_position] == '@')
        {
            _position++;
            while (_position < _input.Length && char.IsLetter(_input[_position]))
            {
                _position++;
            }

            return FilterToken.Operator(_input[start.._position], start);
        }

        // Two-character operators: ==, !=, >=, <=
        if (_position + 1 < _input.Length)
        {
            var twoChar = _input.Substring(_position, 2);
            if (twoChar is "==" or "!=" or ">=" or "<=")
            {
                _position += 2;
                return FilterToken.Operator(twoChar, start);
            }
        }

        // Single character operators: >, <
        if (_input[_position] is '>' or '<')
        {
            return FilterToken.Operator(_input[_position++].ToString(), start);
        }

        throw new FormatException($"Invalid operator at position {_position}");
    }

    private FilterToken ReadIdentifierOrKeyword()
    {
        var start = _position;
        while (_position < _input.Length && (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_'))
        {
            _position++;
        }

        var value = _input[start.._position];

        // Keywords
        return value.ToLower() switch
        {
            "true" => FilterToken.Boolean(true, start),
            "false" => FilterToken.Boolean(false, start),
            "null" => FilterToken.Null(start),
            "and" or "or" or "not" => FilterToken.Operator(value.ToLower(), start),
            _ => FilterToken.Identifier(value, start)
        };
    }

    private bool IsOperatorStart(char ch)
    {
        return ch is '@' or '=' or '!' or '>' or '<';
    }
}
