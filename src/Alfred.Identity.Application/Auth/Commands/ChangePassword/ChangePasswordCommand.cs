using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(UserId UserId, string OldPassword, string NewPassword) : IRequest<Result<bool>>;
