using System.Linq.Expressions;

using Alfred.Identity.Application.Common.Exceptions;
using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Querying.Fields;
using Alfred.Identity.Application.Querying.Filtering.Binding;
using Alfred.Identity.Application.Querying.Filtering.Parsing;
using Alfred.Identity.Application.Querying.Projection;
using Alfred.Identity.Application.Users.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Application.Users.Queries.GetUsers;

/// <summary>
/// Handler for GetUsersQuery
/// </summary>
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PageResult<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IFilterParser _filterParser;

    public GetUsersQueryHandler(
        IUserRepository userRepository,
        IFilterParser filterParser)
    {
        _userRepository = userRepository;
        _filterParser = filterParser;
    }

    public async Task<PageResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var fieldMap = UserFieldMap.Instance;
        var queryRequest = request.QueryRequest;

        var page = queryRequest.GetEffectivePage();
        var pageSize = queryRequest.GetEffectivePageSize();

        // Get view from request (or use default)
        var view = UserFieldMap.Views.GetView(queryRequest.View);

        Expression<Func<User, bool>>? filterExpression = null;
        if (!string.IsNullOrWhiteSpace(queryRequest.Filter))
        {
            try
            {
                var ast = _filterParser.Parse(queryRequest.Filter);
                filterExpression = EfFilterBinder<User>.Bind(ast, fieldMap.Fields);
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
        var fieldSelector = new Func<string, (Expression<Func<User, object>>? Expression, bool CanSort)>(fieldName =>
        {
            if (fieldMap.Fields.TryGet(fieldName, out var expression, out _))
            {
                var canSort = fieldMap.Fields.CanSort(fieldName);
                var objectExpression = ExpressionConverterHelper.ConvertToObjectExpression<User>(expression);
                return (objectExpression, canSort);
            }

            return (null, false);
        });

        // Build paged query and materialize
        var (query, total) = await _userRepository.BuildPagedQueryAsync(
            filterExpression,
            queryRequest.Sort,
            page,
            pageSize,
            view.Includes,
            fieldSelector,
            cancellationToken
        );

        // Apply projection at DB level with AsNoTracking for read-only queries
        var projectedQuery = ProjectionBinder.ApplyProjection(
            query.AsNoTracking(),
            view,
            fieldMap.Fields);

        var items = await projectedQuery.ToListAsync(cancellationToken);

        return new PageResult<UserDto>(items, page, pageSize, total);
    }
}
