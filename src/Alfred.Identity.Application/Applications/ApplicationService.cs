using System.Security.Cryptography;

using Alfred.Identity.Application.Applications.Common;
using Alfred.Identity.Application.Common;
using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Querying.Filtering.Parsing;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;

namespace Alfred.Identity.Application.Applications;

public sealed class ApplicationService : BaseEntityService, IApplicationService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IScopeRepository _scopeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ApplicationService(
        IApplicationRepository applicationRepository,
        IScopeRepository scopeRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IFilterParser filterParser) : base(filterParser)
    {
        _applicationRepository = applicationRepository;
        _scopeRepository = scopeRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    #region Query

    public async Task<PageResult<ApplicationDto>> GetAllApplicationsAsync(QueryRequest query,
        CancellationToken ct = default)
    {
        return await GetPagedAsync(_applicationRepository, query, ApplicationFieldMap.Instance,
            a => ApplicationDto.FromEntity(a), ct);
    }

    public async Task<ApplicationDto?> GetApplicationByIdAsync(Guid id, CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetByIdAsync(new ApplicationId(id), ct);
        return app == null ? null : ApplicationDto.FromEntity(app);
    }

    public async Task<ApplicationMetadataDto> GetMetadataAsync(CancellationToken ct = default)
    {
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

        var scopes = await _scopeRepository.GetAllActiveAsync(ct);
        var scopeList = scopes.Select(s => $"scp:{s.Name}").OrderBy(s => s).ToList();

        return new ApplicationMetadataDto(applicationTypes, clientTypes, grantTypes, scopeList, endpoints);
    }

    #endregion

    #region Commands

    public async Task<ApplicationDto> CreateApplicationAsync(
        string clientId,
        string displayName,
        string redirectUris,
        string postLogoutRedirectUris,
        string permissions,
        string type,
        CancellationToken ct = default)
    {
        var existing = await _applicationRepository.GetByClientIdAsync(clientId, ct);
        if (existing != null)
        {
            throw new InvalidOperationException("Client ID already exists");
        }

        string? secretHash = null;
        string? rawSecret = null;
        if (type == "confidential")
        {
            var secretBytes = RandomNumberGenerator.GetBytes(32);
            rawSecret = Convert.ToBase64String(secretBytes);
            secretHash = _passwordHasher.HashPassword(rawSecret);
        }

        var app = Domain.Entities.Application.Create(
            clientId,
            clientSecret: secretHash,
            displayName: displayName,
            redirectUris: redirectUris,
            postLogoutRedirectUris: postLogoutRedirectUris,
            permissions: permissions,
            clientType: type,
            createdById: _currentUser.UserId
        );

        await _applicationRepository.AddAsync(app, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = ApplicationDto.FromEntity(app);
        return rawSecret != null ? dto with { ClientSecret = rawSecret } : dto;
    }

    public async Task<ApplicationDto> UpdateApplicationAsync(
        Guid id,
        string displayName,
        string redirectUris,
        string? postLogoutRedirectUris,
        string? permissions,
        CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetByIdAsync(new ApplicationId(id), ct)
                  ?? throw new KeyNotFoundException($"Application with ID {id} not found");

        app.Update(
            displayName,
            redirectUris,
            postLogoutRedirectUris ?? string.Empty,
            permissions ?? string.Empty,
            app.ClientType,
            _currentUser.UserId
        );

        _applicationRepository.Update(app);
        await _unitOfWork.SaveChangesAsync(ct);

        return ApplicationDto.FromEntity(app);
    }

    public async Task DeleteApplicationAsync(Guid id, CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetByIdAsync(new ApplicationId(id), ct)
                  ?? throw new KeyNotFoundException($"Application with ID {id} not found");

        _applicationRepository.Delete(app);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetByIdAsync(new ApplicationId(id), ct);
        if (app == null)
        {
            return false;
        }

        app.SetStatus(isActive);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<string> RegenerateClientSecretAsync(Guid id, CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetByIdAsync(new ApplicationId(id), ct)
                  ?? throw new KeyNotFoundException($"Application with ID {id} not found");

        var secretBytes = RandomNumberGenerator.GetBytes(32);
        var rawSecret = Convert.ToBase64String(secretBytes);
        var secretHash = _passwordHasher.HashPassword(rawSecret);

        app.RotateSecret(secretHash);
        await _unitOfWork.SaveChangesAsync(ct);

        return rawSecret;
    }

    #endregion
}
