using Alfred.Identity.Application.Applications.Common;
using Alfred.Identity.Application.Querying;

using MediatR;

namespace Alfred.Identity.Application.Applications.Queries.GetApplications;

/// <summary>
/// Query to get paginated list of applications
/// </summary>
public record GetApplicationsQuery(QueryRequest QueryRequest) : IRequest<PageResult<ApplicationDto>>;
