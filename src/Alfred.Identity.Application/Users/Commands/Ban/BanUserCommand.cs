using MediatR;

namespace Alfred.Identity.Application.Users.Commands.Ban;

public record BanUserCommand(Guid UserId, string Reason, DateTime? ExpiresAt = null) : IRequest<BanUserResult>;

public record BanUserResult(bool IsSuccess, string? Error = null);
