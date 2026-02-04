namespace Alfred.Identity.WebApi.Contracts.Users;

/// <summary>
/// Request model for banning a user
/// </summary>
public record BanUserRequest(string Reason, DateTime? ExpiresAt = null);
