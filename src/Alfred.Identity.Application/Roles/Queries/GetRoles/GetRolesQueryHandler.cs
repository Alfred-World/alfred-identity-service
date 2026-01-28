using System.Linq.Expressions;

using Alfred.Identity.Application.Common.Exceptions;
using Alfred.Identity.Application.Querying;
using Alfred.Identity.Application.Querying.Binding;
using Alfred.Identity.Application.Querying.Parsing;
using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Roles.Queries.GetRoles;

/// <summary>
/// Handler for GetRolesQuery
/// </summary>
public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, PageResult<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IFilterParser _filterParser;

    public GetRolesQueryHandler(
        IRoleRepository roleRepository,
        IFilterParser filterParser)
    {
        _roleRepository = roleRepository;
        _filterParser = filterParser;
    }

    public async Task<PageResult<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var fieldMap = RoleFieldMap.Instance;
        var queryRequest = request.QueryRequest;

        var page = queryRequest.GetEffectivePage();
        var pageSize = queryRequest.GetEffectivePageSize();

        Expression<Func<Role, bool>>? filterExpression = null;
        if (!string.IsNullOrWhiteSpace(queryRequest.Filter))
        {
            try
            {
                var ast = _filterParser.Parse(queryRequest.Filter);
                filterExpression = EfFilterBinder<Role>.Bind(ast, fieldMap.Fields);
            }
            catch (InvalidOperationException ex)
            {
                throw FilterExceptionHelper.CreateFilterException(ex, queryRequest.Filter, fieldMap.Fields);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid filter syntax: {ex.Message}", ex);
            }
        }

        // Create field selector from FieldMap
        var fieldSelector = new Func<string, (Expression<Func<Role, object>>? Expression, bool CanSort)>(fieldName =>
        {
            if (fieldMap.Fields.TryGet(fieldName, out var expression, out _))
            {
                var canSort = fieldMap.Fields.CanSort(fieldName);
                var objectExpression = ExpressionConverterHelper.ConvertToObjectExpression<Role>(expression);
                return (objectExpression, canSort);
            }

            return (null, false);
        });

        var (items, total) = await _roleRepository.GetPagedAsync(
            filterExpression,
            queryRequest.Sort,
            page,
            pageSize,
            null,
            fieldSelector,
            cancellationToken
        );

        var dtos = items.Select(RoleDto.FromEntity).ToList();

        return new PageResult<RoleDto>(dtos, page, pageSize, total);
    }
}
