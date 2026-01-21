using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Applications.Commands.Delete;

/// <summary>
/// Command to delete an application
/// </summary>
public record DeleteApplicationCommand(long Id) : IRequest<Result<bool>>;
