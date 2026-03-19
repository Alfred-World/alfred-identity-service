using Alfred.Identity.Domain.Common.Events;
using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Tests.Entities;

public class UserDomainEventsTests
{
    [Fact]
    public void Ban_ShouldAdd_UserBannedDomainEvent()
    {
        // Arrange
        var user = User.Create("user@example.com", "hash", "Test User");

        // Act
        user.Ban("test", Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        // Assert
        Assert.Contains(user.DomainEvents, e => e is UserBannedDomainEvent);
    }

    [Fact]
    public void Unban_ShouldAdd_UserUnbannedDomainEvent()
    {
        // Arrange
        var user = User.Create("user@example.com", "hash", "Test User");
        user.Ban("test", Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        user.ClearDomainEvents();

        // Act
        user.Unban(Guid.NewGuid());

        // Assert
        Assert.Contains(user.DomainEvents, e => e is UserUnbannedDomainEvent);
    }
}
