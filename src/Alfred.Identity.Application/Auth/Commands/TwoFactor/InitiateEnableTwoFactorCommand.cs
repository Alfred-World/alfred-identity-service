using Alfred.Identity.Application.Common;
using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.TwoFactor;

public record InitiateEnableTwoFactorResult(string Secret, string QrCodeUri);

public record InitiateEnableTwoFactorCommand(Guid UserId, string Email) : IRequest<Result<InitiateEnableTwoFactorResult>>;
