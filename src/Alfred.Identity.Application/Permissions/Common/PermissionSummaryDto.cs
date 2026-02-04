namespace Alfred.Identity.Application.Permissions.Common;

/// <summary>
/// Lightweight DTO for permission - only essential fields for list views.
/// Reduces memory and network transfer compared to full PermissionDto.
/// </summary>
public sealed record PermissionSummaryDto(
    Guid Id,
    string Code,
    string Name
);
