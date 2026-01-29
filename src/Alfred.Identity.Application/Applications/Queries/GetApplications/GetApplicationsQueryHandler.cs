using System.Linq.Expressions;

using Alfred.Identity.Application.Applications.Common;
using Alfred.Identity.Application.Common.Exceptions;
using Alfred.Identity.Application.Querying;
using Alfred.Identity.Application.Querying.Binding;
using Alfred.Identity.Application.Querying.Parsing;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

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
                // If DSL parsing fails, fallback to simple search or throw depending on strictness.
                // The reference throws, so we will throw nicely wrapped exceptions.
                throw FilterExceptionHelper.CreateFilterException(ex, queryRequest.Filter, fieldMap.Fields);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid filter syntax: {ex.Message}", ex);
            }
        }

        // If no complex filter, check if we want to keep the old simple search behavior as fallback?
        // The reference implementation seems to RELY on DSL. 
        // However, existing simple string search might be broken by this if the frontend doesn't send DSL.
        // BUT user asked to match "Sites" exactly.


        // Create field selector from FieldMap to avoid duplication
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

        // Get paginated applications using IRepository's GetPagedAsync
        var (items, total) = await _applicationRepository.GetPagedAsync(
            filterExpression,
            queryRequest.Sort,
            page,
            pageSize,
            null,
            fieldSelector,
            cancellationToken
        );

        var dtos = items.Select(ApplicationDto.FromEntity).ToList();

        return new PageResult<ApplicationDto>(dtos, page, pageSize, total);
    }
}
