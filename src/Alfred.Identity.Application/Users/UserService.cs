using Alfred.Identity.Application.Common;
using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Querying.Filtering.Parsing;
using Alfred.Identity.Application.Users.Common;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Users;

public sealed class UserService : BaseEntityService, IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserActivityLogger _activityLogger;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIdentityUserReplicationEventPublisher _replicationEventPublisher;

    public UserService(
        IUnitOfWork unitOfWork,
        IUserActivityLogger activityLogger,
        ICurrentUser currentUser,
        IFilterParser filterParser,
        IPasswordHasher passwordHasher,
        IIdentityUserReplicationEventPublisher replicationEventPublisher,
        IAsyncQueryExecutor executor) : base(filterParser, executor)
    {
        _unitOfWork = unitOfWork;
        _activityLogger = activityLogger;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _replicationEventPublisher = replicationEventPublisher;
    }

    #region Users

    public async Task<PageResult<UserDto>> GetAllUsersAsync(QueryRequest query, CancellationToken ct = default)
    {
        return await GetPagedWithViewAsync(_unitOfWork.Users, query, UserFieldMap.Instance,
            UserFieldMap.Views, u => UserDto.FromEntity(u), ct);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdWithRolesAsync((UserId)id, ct);
        return user == null ? null : UserDto.FromEntity(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserInput input, CancellationToken ct = default)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        var fullName = input.FullName.Trim();
        var userName = string.IsNullOrWhiteSpace(input.UserName) ? email : input.UserName.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Full name is required");
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            throw new InvalidOperationException("Password is required");
        }

        if (await _unitOfWork.Users.EmailExistsAsync(email, ct))
        {
            throw new InvalidOperationException("Email already registered");
        }

        if (await _unitOfWork.Users.GetByUsernameAsync(userName, ct) != null)
        {
            throw new InvalidOperationException("Username already exists");
        }

        var passwordHash = _passwordHasher.HashPassword(input.Password);
        var user = User.CreateWithUsername(email, userName, passwordHash, fullName, false, _currentUser.UserId);

        var roleIds = input.RoleIds?.Distinct().ToList() ?? new List<Guid>();
        foreach (var roleId in roleIds)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId, ct)
                       ?? throw new KeyNotFoundException($"Role with ID {roleId} not found");
            user.AddRole(role.Id, _currentUser.UserId);
        }

        await _unitOfWork.Users.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _replicationEventPublisher.PublishUserUpsertedAsync(
            user.Id.Value,
            user.UserName,
            user.Email,
            user.FullName,
            user.Avatar,
            user.Status.ToString(),
            user.IsBanned,
            user.IsDeleted,
            ct);

        await _activityLogger.LogAsync(
            user.Id.Value,
            "AdminCreateUser",
            $"User created by admin {_currentUser.Username}",
            ct);

        var createdUser = await _unitOfWork.Users.GetByIdWithRolesAsync(user.Id, ct) ?? user;

        return UserDto.FromEntity(createdUser);
    }

    #endregion

    #region Roles

    public async Task AssignRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdWithRolesAsync((UserId)userId, ct)
                   ?? throw new KeyNotFoundException($"User with ID {userId} not found");

        foreach (var roleId in roleIds)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId, ct)
                       ?? throw new KeyNotFoundException($"Role with ID {roleId} not found");
            _ = role; // validate exists
            user.AddRole(roleId, _currentUser.UserId);
        }

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RevokeRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdWithRolesAsync((UserId)userId, ct)
                   ?? throw new KeyNotFoundException($"User with ID {userId} not found");

        foreach (var roleId in roleIds)
        {
            user.RemoveRole(roleId);
        }

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    #endregion

    #region Ban

    public async Task BanUserAsync(Guid userId, string reason, DateTime? expiresAt, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, ct)
                   ?? throw new KeyNotFoundException($"User with ID {userId} not found");

        if (user.IsBanned)
        {
            throw new InvalidOperationException("User is already banned");
        }

        user.Ban(reason, _currentUser.UserId, expiresAt);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
        await _replicationEventPublisher.PublishUserUpsertedAsync(
            user.Id.Value,
            user.UserName,
            user.Email,
            user.FullName,
            user.Avatar,
            user.Status.ToString(),
            user.IsBanned,
            user.IsDeleted,
            ct);

        await _activityLogger.LogAsync(userId, "BanUser",
            $"Banned by {_currentUser.Username}. Reason: {reason}", ct);
    }

    public async Task UnbanUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, ct)
                   ?? throw new KeyNotFoundException($"User with ID {userId} not found");

        if (!user.IsBanned)
        {
            throw new InvalidOperationException("User is not banned");
        }

        user.Unban(_currentUser.UserId);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
        await _replicationEventPublisher.PublishUserUpsertedAsync(
            user.Id.Value,
            user.UserName,
            user.Email,
            user.FullName,
            user.Avatar,
            user.Status.ToString(),
            user.IsBanned,
            user.IsDeleted,
            ct);

        await _activityLogger.LogAsync(userId, "UnbanUser",
            $"Unbanned by {_currentUser.Username}", ct);
    }

    public async Task<List<BanDto>> GetBanHistoryAsync(Guid userId, CancellationToken ct = default)
    {
        var history = await _unitOfWork.UserBans.GetHistoryByUserIdAsync(userId, ct);
        return history.Select(b => BanDto.FromEntity(b)).ToList();
    }

    #endregion

    #region Activity

    public async Task<ActivityLogPageResult> GetActivityLogsAsync(Guid userId, int page, int pageSize,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _unitOfWork.UserActivityLogs.GetPagedAsync(userId, page, pageSize, ct);
        var dtos = items.Select(l => ActivityLogDto.FromEntity(l)).ToList();
        return new ActivityLogPageResult(dtos, totalCount, page, pageSize);
    }

    #endregion

    #region Admin Password Management

    public async Task AdminResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, ct)
                   ?? throw new KeyNotFoundException($"User with ID {userId} not found");

        var hash = _passwordHasher.HashPassword(newPassword);
        user.SetPassword(hash);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        await _activityLogger.LogAsync(userId, "AdminResetPassword",
            $"Password reset by admin {_currentUser.Username}", ct);
    }

    public async Task AdminConfirmEmailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, ct)
                   ?? throw new KeyNotFoundException($"User with ID {userId} not found");

        if (user.EmailConfirmed)
        {
            return;
        }

        user.ConfirmEmail(_currentUser.UserId);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        await _activityLogger.LogAsync(userId, "AdminConfirmEmail",
            $"Email confirmed by admin {_currentUser.Username}", ct);
    }

    #endregion
}
