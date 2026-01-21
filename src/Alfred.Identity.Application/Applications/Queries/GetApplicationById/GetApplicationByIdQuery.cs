using Alfred.Identity.Application.Applications.Shared;
using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Applications.Queries.GetApplicationById;

/// <summary>
/// Query to get application by ID
/// </summary>
public record GetApplicationByIdQuery(long Id) : IRequest<Result<ApplicationDto>>;
