using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.Rotation;

public record RotateSigningKeyCommand : IRequest<RotateSigningKeyResult>;

public record RotateSigningKeyResult(bool Success, string NewKeyId, string? Error = null);
