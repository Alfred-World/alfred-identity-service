using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.UpdateProfile;

public sealed record UpdateProfileCommand : IRequest<Result<UpdateProfileResult>>
{
    public required UserId UserId { get; init; }
    public Optional<string> FullName { get; init; }
    public Optional<string?> PhoneNumber { get; init; }
    public Optional<string?> Avatar { get; init; }
}

public record UpdateProfileResult(
    Guid Id,
    string FullName,
    string? PhoneNumber,
    string? Avatar,
    string Email,
    string UserName
);
