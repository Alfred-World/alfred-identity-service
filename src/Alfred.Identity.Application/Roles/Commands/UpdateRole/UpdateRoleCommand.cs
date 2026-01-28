using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.UpdateRole;

public record UpdateRoleCommand(long Id, string Name) : IRequest<UpdateRoleResult>;

public record UpdateRoleResult(bool Success, string? Error = null);
