using Alfred.Identity.Application.Common;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Applications.Commands.Delete;

/// <summary>
/// Handler for DeleteApplicationCommand
/// </summary>
public class DeleteApplicationCommandHandler : IRequestHandler<DeleteApplicationCommand, Result<bool>>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteApplicationCommandHandler(
        IApplicationRepository applicationRepository,
        IUnitOfWork unitOfWork)
    {
        _applicationRepository = applicationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.Id, cancellationToken);

        if (application == null)
        {
            return Result<bool>.Failure($"Application with ID {request.Id} not found");
        }

        // Use sync Delete method from IRepository
        _applicationRepository.Delete(application);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
