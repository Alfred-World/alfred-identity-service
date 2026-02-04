using System.Linq.Expressions;

using Alfred.Identity.Application.Common.Exceptions;
using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Querying.Fields;
using Alfred.Identity.Application.Querying.Filtering.Binding;
using Alfred.Identity.Application.Querying.Filtering.Parsing;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Application.Permissions.Queries.GetPermissions;

/// <summary>
/// Handler for GetPermissionsQuery
/// </summary>
public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, PageResult<PermissionDto>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IFilterParser _filterParser;

    public GetPermissionsQueryHandler(
        IPermissionRepository permissionRepository,
        IFilterParser filterParser)
    {
        _permissionRepository = permissionRepository;
        _filterParser = filterParser;
    }

    public async Task<PageResult<PermissionDto>> Handle(GetPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var fieldMap = PermissionFieldMap.Instance;
        var queryRequest = request.QueryRequest;

        var page = queryRequest.GetEffectivePage();
        var pageSize = queryRequest.GetEffectivePageSize();

        Expression<Func<Permission, bool>>? filterExpression = null;
        if (!string.IsNullOrWhiteSpace(queryRequest.Filter))
        {
            try
            {
                var ast = _filterParser.Parse(queryRequest.Filter);
                filterExpression = EfFilterBinder<Permission>.Bind(ast, fieldMap.Fields);
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
        var fieldSelector =
            new Func<string, (Expression<Func<Permission, object>>? Expression, bool CanSort)>(fieldName =>
            {
                if (fieldMap.Fields.TryGet(fieldName, out var expression, out _))
                {
                    var canSort = fieldMap.Fields.CanSort(fieldName);
                    var objectExpression = ExpressionConverterHelper.ConvertToObjectExpression<Permission>(expression);
                    return (objectExpression, canSort);
                }

                return (null, false);
            });

        // Build paged query and materialize
        var (query, total) = await _permissionRepository.BuildPagedQueryAsync(
            filterExpression,
            queryRequest.Sort,
            page,
            pageSize,
            null,
            fieldSelector,
            cancellationToken
        );

        // Use AsNoTracking for read-only queries to reduce memory overhead
        var items = await query.AsNoTracking().ToListAsync(cancellationToken);
        var dtos = items.Select(PermissionDto.FromEntity).ToList();

        return new PageResult<PermissionDto>(dtos, page, pageSize, total);
    }
}
