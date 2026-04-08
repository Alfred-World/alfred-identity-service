using Alfred.Identity.Application.Users.Common;
using Alfred.Identity.Domain.Querying;

namespace Alfred.Identity.Application.Users;

public interface IUserService
{
    #region Users

    Task<PageResult<UserDto>> SearchUsersAsync(SearchRequest request, CancellationToken ct = default);
    SearchMetadataResponse GetSearchMetadata();
    Task<UserDto?> GetUserByIdAsync(UserId id, CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(CreateUserInput input, CancellationToken ct = default);

    #endregion

    #region Roles

    Task AssignRolesAsync(UserId userId, IEnumerable<RoleId> roleIds, CancellationToken ct = default);
    Task RevokeRolesAsync(UserId userId, IEnumerable<RoleId> roleIds, CancellationToken ct = default);

    #endregion

    #region Ban

    Task BanUserAsync(UserId userId, string reason, DateTime? expiresAt, CancellationToken ct = default);
    Task UnbanUserAsync(UserId userId, CancellationToken ct = default);
    Task<List<BanDto>> GetBanHistoryAsync(UserId userId, CancellationToken ct = default);

    #endregion

    #region Activity

    Task<ActivityLogPageResult> GetActivityLogsAsync(UserId userId, int page, int pageSize,
        CancellationToken ct = default);

    #endregion

    #region Admin Password Management

    /// <summary>Admin resets a user's password directly without requiring their old password.</summary>
    Task AdminResetPasswordAsync(UserId userId, string newPassword, CancellationToken ct = default);

    /// <summary>Admin confirms a user's email without token flow.</summary>
    Task AdminConfirmEmailAsync(UserId userId, CancellationToken ct = default);

    #endregion
}
