using MediatR;

namespace Alfred.Identity.Application.Users.Commands.Unban;

public record UnbanUserCommand(Guid UserId) : IRequest<UnbanUserResult>;

public record UnbanUserResult(bool IsSuccess, string? Error = null);
