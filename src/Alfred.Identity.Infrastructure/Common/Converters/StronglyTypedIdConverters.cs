using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Alfred.Identity.Infrastructure.Common.Converters;

public sealed class UserIdConverter() : ValueConverter<UserId, Guid>(id => id.Value, g => new UserId(g));

public sealed class RoleIdConverter() : ValueConverter<RoleId, Guid>(id => id.Value, g => new RoleId(g));

public sealed class PermissionIdConverter()
    : ValueConverter<PermissionId, Guid>(id => id.Value, g => new PermissionId(g));

public sealed class TokenIdConverter() : ValueConverter<TokenId, Guid>(id => id.Value, g => new TokenId(g));

public sealed class ApplicationIdConverter()
    : ValueConverter<ApplicationId, Guid>(id => id.Value, g => new ApplicationId(g));

public sealed class AuthorizationIdConverter()
    : ValueConverter<AuthorizationId, Guid>(id => id.Value, g => new AuthorizationId(g));

public sealed class ScopeIdConverter() : ValueConverter<ScopeId, Guid>(id => id.Value, g => new ScopeId(g));

public sealed class SigningKeyIdConverter()
    : ValueConverter<SigningKeyId, Guid>(id => id.Value, g => new SigningKeyId(g));

public sealed class UserBanIdConverter() : ValueConverter<UserBanId, Guid>(id => id.Value, g => new UserBanId(g));

public sealed class UserActivityLogIdConverter()
    : ValueConverter<UserActivityLogId, Guid>(id => id.Value, g => new UserActivityLogId(g));

public sealed class UserLoginIdConverter() : ValueConverter<UserLoginId, Guid>(id => id.Value, g => new UserLoginId(g));

public sealed class BackupCodeIdConverter()
    : ValueConverter<BackupCodeId, Guid>(id => id.Value, g => new BackupCodeId(g));
