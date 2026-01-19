using Alfred.Identity.Application.Querying.Ast;

namespace Alfred.Identity.Application.Querying.Parsing;

/// <summary>
/// Interface cho filter parser
/// </summary>
public interface IFilterParser
{
    FilterNode Parse(string filter);
}
