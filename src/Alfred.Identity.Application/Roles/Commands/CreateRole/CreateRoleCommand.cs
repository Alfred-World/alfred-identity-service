using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.CreateRole;

public record CreateRoleCommand(string Name) : IRequest<CreateRoleResult>;

public record CreateRoleResult(bool Success, long? RoleId = null, string? Error = null);
