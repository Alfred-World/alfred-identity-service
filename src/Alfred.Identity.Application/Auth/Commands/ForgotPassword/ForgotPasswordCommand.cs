using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<Result<bool>>;
