using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.UpdateRole;

public record UpdateRoleCommand(Guid Id, string Name, string? Icon = null) : IRequest<UpdateRoleResult>;

public record UpdateRoleResult(bool Success, string? Error = null);
