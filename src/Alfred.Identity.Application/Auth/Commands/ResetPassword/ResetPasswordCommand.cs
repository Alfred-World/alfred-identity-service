using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<Result<bool>>;
