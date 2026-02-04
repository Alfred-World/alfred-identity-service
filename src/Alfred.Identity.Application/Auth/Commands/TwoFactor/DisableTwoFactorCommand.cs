using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.TwoFactor;

public record DisableTwoFactorCommand(Guid UserId) : IRequest<Result<bool>>;
