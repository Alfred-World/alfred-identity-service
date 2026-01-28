using Alfred.Identity.Application.Applications.Common;
using Alfred.Identity.Application.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Applications.Queries.GetApplicationById;

/// <summary>
/// Handler for GetApplicationByIdQuery
/// </summary>
public class GetApplicationByIdQueryHandler : IRequestHandler<GetApplicationByIdQuery, Result<ApplicationDto>>
{
    private readonly IApplicationRepository _applicationRepository;

    public GetApplicationByIdQueryHandler(IApplicationRepository applicationRepository)
    {
        _applicationRepository = applicationRepository;
    }

    public async Task<Result<ApplicationDto>> Handle(GetApplicationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.Id, cancellationToken);

        if (application == null)
        {
            return Result<ApplicationDto>.Failure($"Application with ID {request.Id} not found");
        }

        return Result<ApplicationDto>.Success(ApplicationDto.FromEntity(application));
    }
}
