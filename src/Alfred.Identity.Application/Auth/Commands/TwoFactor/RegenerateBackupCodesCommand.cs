using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.TwoFactor;

/// <summary>
/// Regenerate 10 new single-use recovery codes, invalidating all existing ones.
/// </summary>
/// <param name="UserId">The authenticated user requesting regeneration.</param>
public record RegenerateBackupCodesCommand(UserId UserId) : IRequest<Result<IEnumerable<string>>>;
