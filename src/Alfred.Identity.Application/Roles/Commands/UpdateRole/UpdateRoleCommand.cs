using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.UpdateRole;

public record UpdateRoleCommand(long Id, string Name, string? Icon = null) : IRequest<UpdateRoleResult>;

public record UpdateRoleResult(bool Success, string? Error = null);
