using MediatR;

namespace Alfred.Identity.Application.Applications.Commands.UpdateStatus;

public record UpdateApplicationStatusCommand(Guid Id, bool IsActive) : IRequest<bool>;
