using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.DeleteRole;

public record DeleteRoleCommand(long Id) : IRequest<DeleteRoleResult>;

public record DeleteRoleResult(bool Success, string? Error = null);
