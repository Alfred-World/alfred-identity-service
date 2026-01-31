using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.CreateRole;

public record CreateRoleCommand(string Name, string? Icon = null) : IRequest<CreateRoleResult>;

public record CreateRoleResult(bool Success, Guid? RoleId = null, string? Error = null);
