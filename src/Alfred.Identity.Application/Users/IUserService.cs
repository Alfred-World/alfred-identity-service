using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Users.Common;

namespace Alfred.Identity.Application.Users;

public interface IUserService
{
    #region Users

    Task<PageResult<UserDto>> GetAllUsersAsync(QueryRequest query, CancellationToken ct = default);
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(CreateUserInput input, CancellationToken ct = default);

    #endregion

    #region Roles

    Task AssignRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken ct = default);
    Task RevokeRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken ct = default);

    #endregion

    #region Ban

    Task BanUserAsync(Guid userId, string reason, DateTime? expiresAt, CancellationToken ct = default);
    Task UnbanUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<BanDto>> GetBanHistoryAsync(Guid userId, CancellationToken ct = default);

    #endregion

    #region Activity

    Task<ActivityLogPageResult> GetActivityLogsAsync(Guid userId, int page, int pageSize,
        CancellationToken ct = default);

    #endregion

    #region Admin Password Management

    /// <summary>Admin resets a user's password directly without requiring their old password.</summary>
    Task AdminResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);

    /// <summary>Admin confirms a user's email without token flow.</summary>
    Task AdminConfirmEmailAsync(Guid userId, CancellationToken ct = default);

    #endregion
}
