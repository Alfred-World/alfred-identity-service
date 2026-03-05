using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string FullName,
    string? PhoneNumber,
    string? Avatar
) : IRequest<Result<UpdateProfileResult>>;

public record UpdateProfileResult(
    Guid Id,
    string FullName,
    string? PhoneNumber,
    string? Avatar,
    string Email,
    string UserName
);
