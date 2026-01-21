using Alfred.Identity.Application.Applications.Shared;
using Alfred.Identity.Application.Querying;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Applications.Queries.GetApplications;

/// <summary>
/// Handler for GetApplicationsQuery
/// </summary>
public class GetApplicationsQueryHandler : IRequestHandler<GetApplicationsQuery, PageResult<ApplicationDto>>
{
    private readonly IApplicationRepository _applicationRepository;

    public GetApplicationsQueryHandler(IApplicationRepository applicationRepository)
    {
        _applicationRepository = applicationRepository;
    }

    public async Task<PageResult<ApplicationDto>> Handle(GetApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = request.QueryRequest;
        var page = query.GetEffectivePage();
        var pageSize = query.GetEffectivePageSize();

        // Get paginated applications using IRepository's GetPagedAsync
        var (items, total) = await _applicationRepository.GetPagedAsync(
            null,
            query.Sort,
            page,
            pageSize,
            null,
            null,
            cancellationToken
        );

        var dtos = items.ToDtos().ToList();

        return new PageResult<ApplicationDto>(dtos, page, pageSize, total);
    }
}
