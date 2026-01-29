using Alfred.Identity.Application.Applications.Common;
using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Applications.Queries.GetMetadata;

public record GetApplicationMetadataQuery : IRequest<Result<ApplicationMetadataDto>>;
