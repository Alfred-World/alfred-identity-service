using System.Linq.Expressions;

using Alfred.Identity.Application.Common.Exceptions;
using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Querying.Fields;
using Alfred.Identity.Application.Querying.Filtering.Binding;
using Alfred.Identity.Application.Querying.Filtering.Parsing;
using Alfred.Identity.Application.Querying.Projection;
using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Application.Roles.Queries.GetRoles;

/// <summary>
/// Handler for GetRolesQuery
/// Uses view-based projection for database-level optimization
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

        // Get view from request (or use default)
        var view = RoleFieldMap.Views.GetView(queryRequest.View);

        // Parse filter (at DB level)
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

        // Create field selector from FieldMap (for sorting at DB level)
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

        // Build paged query (filter, sort, paginate at DB level)
        var (query, total) = await _roleRepository.BuildPagedQueryAsync(
            filterExpression,
            queryRequest.Sort,
            page,
            pageSize,
            view.Includes,
            fieldSelector,
            cancellationToken
        );

        // Apply projection at DB level (generates SELECT with only required fields)
        // Uses view definition to support field aliases (e.g., permissionsSummary -> permissions)
        var projectedQuery = ProjectionBinder.ApplyProjection(
            query.AsNoTracking(), // Disable change tracking for read-only queries
            view,
            fieldMap.Fields);

        var items = await projectedQuery.ToListAsync(cancellationToken);

        return new PageResult<RoleDto>(items, page, pageSize, total);
    }
}
