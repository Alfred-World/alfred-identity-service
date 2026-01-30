using Alfred.Identity.Application.Querying.Filtering.Ast;

namespace Alfred.Identity.Application.Querying.Filtering.Parsing;

/// <summary>
/// Interface cho filter parser
/// </summary>
public interface IFilterParser
{
    FilterNode Parse(string filter);
}
