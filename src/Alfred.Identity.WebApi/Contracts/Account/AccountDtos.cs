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

