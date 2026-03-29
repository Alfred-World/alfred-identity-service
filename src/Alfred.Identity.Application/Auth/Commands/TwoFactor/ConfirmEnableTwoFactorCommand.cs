using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.TwoFactor;

public record ConfirmEnableTwoFactorCommand(UserId UserId, string Code) : IRequest<Result<IEnumerable<string>>>;
