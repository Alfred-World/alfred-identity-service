using Alfred.Identity.Application.Common;
using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Querying.Filtering.Parsing;
using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Roles;

public sealed class RoleService : BaseEntityService, IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ICurrentUser _currentUser;

    public RoleService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ICurrentUser currentUser,
        IFilterParser filterParser,
        IAsyncQueryExecutor executor) : base(filterParser, executor)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _currentUser = currentUser;
    }

    #region Queries

    public async Task<PageResult<RoleDto>> GetAllRolesAsync(QueryRequest query,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedWithViewAsync(_roleRepository, query, RoleFieldMap.Instance,
            RoleFieldMap.Views, r => RoleDto.FromEntity(r), cancellationToken);
    }

    public async Task<RoleDto?> GetRoleByIdAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        var entity = await _roleRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : RoleDto.FromEntity(entity);
    }

    public async Task<List<PermissionDto>> GetRolePermissionsAsync(RoleId roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            return [];
        }

        return role.RolePermissions
            .Select(rp => PermissionDto.FromEntity(rp.Permission))
            .ToList();
    }

    #endregion

    #region Commands

    public async Task<RoleDto> CreateRoleAsync(string name, string? icon, bool isImmutable, bool isSystem,
        IEnumerable<Guid>? permissions, CancellationToken cancellationToken = default)
    {
        if (await _roleRepository.ExistsAsync(name, cancellationToken))
        {
            throw new InvalidOperationException($"Role '{name}' already exists.");
        }

        var role = Role.Create(name, icon, isImmutable, isSystem, _currentUser.UserId);

        if (permissions != null)
        {
            foreach (var permissionId in permissions)
            {
                role.AddPermission(permissionId, _currentUser.UserId);
            }
        }

        await _roleRepository.AddAsync(role, cancellationToken);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        var created = await _roleRepository.GetByIdAsync(role.Id, cancellationToken);
        return RoleDto.FromEntity(created!);
    }

    public async Task<RoleDto> UpdateRoleAsync(RoleId id, string name, string? icon, bool isImmutable, bool isSystem,
        IEnumerable<Guid>? permissions, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            throw new KeyNotFoundException($"Role with ID {id} not found.");
        }

        if (role.IsImmutable)
        {
            throw new InvalidOperationException("Cannot modify immutable role.");
        }

        role.Update(name, icon, isImmutable, isSystem);
        role.UpdatedById = _currentUser.UserId;

        if (permissions != null)
        {
            role.SyncPermissions(permissions.Select(p => (PermissionId)p), _currentUser.UserId);
        }

        _roleRepository.Update(role);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        var updated = await _roleRepository.GetByIdAsync(role.Id, cancellationToken);
        return RoleDto.FromEntity(updated!);
    }

    public async Task<RoleDto> DeleteRoleAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            throw new KeyNotFoundException($"Role with ID {id} not found.");
        }

        if (role.IsImmutable)
        {
            throw new InvalidOperationException("Cannot delete immutable role.");
        }

        if (role.IsSystem)
        {
            throw new InvalidOperationException("Cannot delete system role.");
        }

        var dto = RoleDto.FromEntity(role);
        role.DeletedById = _currentUser.UserId;

        _roleRepository.Delete(role);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        return dto;
    }

    public async Task<RoleDto> AddPermissionsToRoleAsync(RoleId roleId, IEnumerable<PermissionId> permissionIds,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            throw new KeyNotFoundException($"Role with ID {roleId} not found.");
        }

        if (role.IsImmutable)
        {
            throw new InvalidOperationException("Cannot modify immutable role.");
        }

        var typedIds = permissionIds.Distinct().ToList();
        var valid =
            await _permissionRepository.FindAsync(p => typedIds.Contains(p.Id), cancellationToken);
        var validIds = valid.Select(p => p.Id).ToHashSet();
        var invalid = typedIds.Where(id => !validIds.Contains(id)).ToList();

        if (invalid.Count > 0)
        {
            throw new InvalidOperationException($"Permissions not found: {string.Join(", ", invalid)}");
        }

        role.SyncPermissions(typedIds, _currentUser.UserId);

        _roleRepository.Update(role);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        var updated = await _roleRepository.GetByIdAsync(role.Id, cancellationToken);
        return RoleDto.FromEntity(updated!);
    }

    public async Task<RoleDto> RemovePermissionsFromRoleAsync(RoleId roleId, IEnumerable<PermissionId> permissionIds,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            throw new KeyNotFoundException($"Role with ID {roleId} not found.");
        }

        if (role.IsImmutable)
        {
            throw new InvalidOperationException("Cannot modify immutable role.");
        }

        var removeIds = permissionIds.ToHashSet();
        var keepIds = role.RolePermissions
            .Select(rp => rp.PermissionId)
            .Where(id => !removeIds.Contains(id))
            .ToList();

        role.SyncPermissions(keepIds, _currentUser.UserId);

        _roleRepository.Update(role);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        var updated = await _roleRepository.GetByIdAsync(role.Id, cancellationToken);
        return RoleDto.FromEntity(updated!);
    }

    #endregion
}
