using System.Linq.Expressions;

using Alfred.Identity.Application.Applications.Common;
using Alfred.Identity.Application.Common.Exceptions;
using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Querying.Fields;
using Alfred.Identity.Application.Querying.Filtering.Binding;
using Alfred.Identity.Application.Querying.Filtering.Parsing;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Application.Applications.Queries.GetApplications;

/// <summary>
/// Handler for GetApplicationsQuery
/// </summary>
public class GetApplicationsQueryHandler : IRequestHandler<GetApplicationsQuery, PageResult<ApplicationDto>>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IFilterParser _filterParser;

    public GetApplicationsQueryHandler(
        IApplicationRepository applicationRepository,
        IFilterParser filterParser)
    {
        _applicationRepository = applicationRepository;
        _filterParser = filterParser;
    }

    public async Task<PageResult<ApplicationDto>> Handle(GetApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var fieldMap = ApplicationFieldMap.Instance;
        var queryRequest = request.QueryRequest;

        var page = queryRequest.GetEffectivePage();
        var pageSize = queryRequest.GetEffectivePageSize();

        Expression<Func<Domain.Entities.Application, bool>>? filterExpression = null;
        if (!string.IsNullOrWhiteSpace(queryRequest.Filter))
        {
            try
            {
                var ast = _filterParser.Parse(queryRequest.Filter);
                filterExpression = EfFilterBinder<Domain.Entities.Application>.Bind(ast, fieldMap.Fields);
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
            new Func<string, (Expression<Func<Domain.Entities.Application, object>>? Expression, bool CanSort
                )>(fieldName =>
            {
                if (fieldMap.Fields.TryGet(fieldName, out var expression, out _))
                {
                    var canSort = fieldMap.Fields.CanSort(fieldName);
                    var objectExpression =
                        ExpressionConverterHelper.ConvertToObjectExpression<Domain.Entities.Application>(expression);
                    return (objectExpression, canSort);
                }

                return (null, false);
            });

        // Build paged query and materialize
        var (query, total) = await _applicationRepository.BuildPagedQueryAsync(
            filterExpression,
            queryRequest.Sort,
            page,
            pageSize,
            null,
            fieldSelector,
            cancellationToken
        );

        var items = await query.ToListAsync(cancellationToken);
        var dtos = items.Select(ApplicationDto.FromEntity).ToList();

        return new PageResult<ApplicationDto>(dtos, page, pageSize, total);
    }
}
