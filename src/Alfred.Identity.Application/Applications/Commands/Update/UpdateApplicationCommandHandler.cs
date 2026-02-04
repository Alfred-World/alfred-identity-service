using Alfred.Identity.Application.Applications.Common;
using Alfred.Identity.Application.Common;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Applications.Commands.Update;

/// <summary>
/// Handler for UpdateApplicationCommand
/// </summary>
public class UpdateApplicationCommandHandler : IRequestHandler<UpdateApplicationCommand, Result<ApplicationDto>>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateApplicationCommandHandler(
        IApplicationRepository applicationRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _applicationRepository = applicationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<ApplicationDto>> Handle(UpdateApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.Id, cancellationToken);

        if (application == null)
        {
            return Result<ApplicationDto>.Failure($"Application with ID {request.Id} not found");
        }

        // Update properties using domain methods
        application.Update(
            request.DisplayName,
            request.RedirectUris,
            request.PostLogoutRedirectUris,
            request.Permissions,
            application.ClientType,
            _currentUser.UserId
        );

        _applicationRepository.Update(application);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ApplicationDto>.Success(ApplicationDto.FromEntity(application));
    }
}

