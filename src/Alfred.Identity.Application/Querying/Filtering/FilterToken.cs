namespace Alfred.Identity.Application.Querying.Filtering;

/// <summary>
/// Token type cho filter parser
/// </summary>
public enum FilterTokenType
{
    EndOfInput,
    Identifier,
    String,
    Number,
    Boolean,
    Null,
    Operator,
    LeftParen,
    RightParen,
    Comma
}

/// <summary>
/// Token trong filter DSL
/// </summary>
public sealed record FilterToken(
    FilterTokenType Type,
    string Value,
    int Position)
{
    public static FilterToken EndOfInput(int position)
    {
        return new FilterToken(FilterTokenType.EndOfInput, string.Empty, position);
    }

    public static FilterToken Identifier(string value, int position)
    {
        return new FilterToken(FilterTokenType.Identifier, value, position);
    }

    public static FilterToken String(string value, int position)
    {
        return new FilterToken(FilterTokenType.String, value, position);
    }

    public static FilterToken Number(string value, int position)
    {
        return new FilterToken(FilterTokenType.Number, value, position);
    }

    public static FilterToken Boolean(bool value, int position)
    {
        return new FilterToken(FilterTokenType.Boolean, value.ToString().ToLower(), position);
    }

    public static FilterToken Null(int position)
    {
        return new FilterToken(FilterTokenType.Null, "null", position);
    }

    public static FilterToken Operator(string value, int position)
    {
        return new FilterToken(FilterTokenType.Operator, value, position);
    }

    public static FilterToken LeftParen(int position)
    {
        return new FilterToken(FilterTokenType.LeftParen, "(", position);
    }

    public static FilterToken RightParen(int position)
    {
        return new FilterToken(FilterTokenType.RightParen, ")", position);
    }

    public static FilterToken Comma(int position)
    {
        return new FilterToken(FilterTokenType.Comma, ",", position);
    }
}
