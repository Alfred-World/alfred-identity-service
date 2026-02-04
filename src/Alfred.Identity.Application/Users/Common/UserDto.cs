using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Users.Common;

/// <summary>
/// Data Transfer Object for User entity
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? Status { get; set; }
    public bool? EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Avatar { get; set; }
    public DateTime? CreatedAt { get; set; }
    public IEnumerable<RoleDto> Roles { get; set; } = new List<RoleDto>();

    public UserDto()
    {
    }

    public static UserDto FromEntity(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            Status = user.Status.ToString(),
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            Avatar = user.Avatar,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles
                .Select(rp => RoleDto.FromEntity(rp.Role))
                .ToList()
        };
    }
}

