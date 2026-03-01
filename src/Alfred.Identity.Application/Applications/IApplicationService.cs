using Alfred.Identity.Application.Applications.Common;
using Alfred.Identity.Application.Querying.Core;

namespace Alfred.Identity.Application.Applications;

public interface IApplicationService
{
    #region Query

    Task<PageResult<ApplicationDto>> GetAllApplicationsAsync(QueryRequest query, CancellationToken ct = default);
    Task<ApplicationDto?> GetApplicationByIdAsync(Guid id, CancellationToken ct = default);
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
        Guid id,
        string displayName,
        string redirectUris,
        string? postLogoutRedirectUris,
        string? permissions,
        CancellationToken ct = default);

    Task DeleteApplicationAsync(Guid id, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task<string> RegenerateClientSecretAsync(Guid id, CancellationToken ct = default);

    #endregion
}
