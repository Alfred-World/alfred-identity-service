using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.RevokeSession;

public record RevokeSessionCommand(
    UserId UserId,
    TokenId TokenId
) : IRequest<Result<object>>;
