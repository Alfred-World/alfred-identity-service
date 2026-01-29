using MediatR;

namespace Alfred.Identity.Application.Applications.Commands.UpdateStatus;

public record UpdateApplicationStatusCommand(long Id, bool IsActive) : IRequest<bool>;
