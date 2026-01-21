using Alfred.Identity.Application.Applications.Shared;
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

    public UpdateApplicationCommandHandler(
        IApplicationRepository applicationRepository,
        IUnitOfWork unitOfWork)
    {
        _applicationRepository = applicationRepository;
        _unitOfWork = unitOfWork;
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
        application.UpdateRedirectUris(request.RedirectUris);

        // Use sync Update method from IRepository
        _applicationRepository.Update(application);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ApplicationDto>.Success(application.ToDto());
    }
}
