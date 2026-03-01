namespace Alfred.Identity.WebApi.Contracts.Users;

/// <summary>
/// Admin-only: force-reset a user's password without requiring their old password.
/// </summary>
public record AdminResetPasswordRequest(string NewPassword);
