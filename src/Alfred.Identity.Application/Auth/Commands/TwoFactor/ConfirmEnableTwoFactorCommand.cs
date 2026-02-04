using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.TwoFactor;

public record ConfirmEnableTwoFactorCommand(Guid UserId, string Code) : IRequest<Result<IEnumerable<string>>>;

