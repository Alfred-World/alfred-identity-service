using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Users.Common;

/// <summary>
/// Data Transfer Object for User entity
/// </summary>
public record UserDto(
    long Id,
    string UserName,
    string Email,
    string FullName,
    string Status,
    bool EmailConfirmed,
    DateTime CreatedAt
)
{
    public static UserDto FromEntity(User user)
    {
        return new UserDto(
            user.Id,
            user.UserName,
            user.Email,
            user.FullName,
            user.Status,
            user.EmailConfirmed,
            user.CreatedAt
        );
    }
}
