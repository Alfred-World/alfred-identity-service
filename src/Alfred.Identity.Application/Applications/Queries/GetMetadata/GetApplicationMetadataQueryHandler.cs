using Alfred.Identity.Application.Applications.Common;
using Alfred.Identity.Application.Common;
using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Applications.Queries.GetMetadata;

public class GetApplicationMetadataQueryHandler : IRequestHandler<GetApplicationMetadataQuery, Result<ApplicationMetadataDto>>
{
    private readonly IScopeRepository _scopeRepository;

    public GetApplicationMetadataQueryHandler(IScopeRepository scopeRepository)
    {
        _scopeRepository = scopeRepository;
    }

    public async Task<Result<ApplicationMetadataDto>> Handle(GetApplicationMetadataQuery request, CancellationToken cancellationToken)
    {
        // 1. Static Metadata (Based on protocol and system support)
        var applicationTypes = new List<string> { "web", "native", "machine", "spa" };
        var clientTypes = new List<string> { "confidential", "public" };
        
        var grantTypes = new List<string> 
        { 
            "gt:authorization_code", 
            "gt:refresh_token", 
            "gt:client_credentials", 
            "gt:password" 
        };

        var endpoints = new List<string>
        {
            "ept:authorization",
            "ept:token",
            "ept:userinfo",
            "ept:introspection",
            "ept:revocation",
            "ept:logout"
        };

        // 2. Dynamic Scopes from DB (prefixed with scp:)
        var scopes = await _scopeRepository.GetAllActiveAsync(cancellationToken);
        var scopeList = scopes.Select(s => $"scp:{s.Name}").OrderBy(s => s).ToList();

        return Result<ApplicationMetadataDto>.Success(new ApplicationMetadataDto(
            applicationTypes,
            clientTypes,
            grantTypes,
            scopeList,
            endpoints
        ));
    }
}
