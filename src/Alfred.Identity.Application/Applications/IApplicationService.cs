using Alfred.Identity.Application.Applications.Common;

namespace Alfred.Identity.Application.Applications;

public interface IApplicationService
{
    #region Query

    Task<PageResult<ApplicationDto>> GetAllApplicationsAsync(QueryRequest query, CancellationToken ct = default);
    Task<ApplicationDto?> GetApplicationByIdAsync(ApplicationId id, CancellationToken ct = default);
    Task<ApplicationMetadataDto> GetMetadataAsync(CancellationToken ct = default);

    #endregion

    #region Commands

    Task<ApplicationDto> CreateApplicationAsync(
        string clientId,
        string displayName,
        string redirectUris,
        string postLogoutRedirectUris,
        string permissions,
        string type,
        CancellationToken ct = default);

    Task<ApplicationDto> UpdateApplicationAsync(
        ApplicationId id,
        string displayName,
        string redirectUris,
        string? postLogoutRedirectUris,
        string? permissions,
        CancellationToken ct = default);

    Task DeleteApplicationAsync(ApplicationId id, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(ApplicationId id, bool isActive, CancellationToken ct = default);
    Task<string> RegenerateClientSecretAsync(ApplicationId id, CancellationToken ct = default);

    #endregion
}
