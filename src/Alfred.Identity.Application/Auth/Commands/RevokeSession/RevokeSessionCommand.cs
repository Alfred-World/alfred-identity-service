using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.RevokeSession;

public record RevokeSessionCommand(
    Guid UserId,
    Guid TokenId
) : IRequest<Result<object>>;
