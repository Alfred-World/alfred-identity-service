namespace Alfred.Identity.WebApi.Contracts.Account;

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ConfirmTwoFactorRequest
{
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Returns how many recovery codes remain unused vs the total generated.
/// </summary>
public record RecoveryCodeStatusResponse(int RemainingCount, int TotalCount);

/// <summary>
/// Current user profile response
/// </summary>
public class ProfileResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Avatar { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool EmailConfirmed { get; set; }
}

/// <summary>
/// Request to update current user's profile
/// </summary>
public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    /// <summary>Base64 encoded image or URL. Null to keep existing.</summary>
    public string? Avatar { get; set; }
}

/// <summary>
/// An active session (refresh token) for the current user
/// </summary>
public class SessionDto
{
    public Guid Id { get; set; }
    public string? Device { get; set; }
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsCurrentSession { get; set; }
}
