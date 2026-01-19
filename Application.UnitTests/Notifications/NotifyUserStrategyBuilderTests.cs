using Application.Builders;
using Application.Models.Notifications;

namespace Application_UnitTests.Notifications;

public class NotifyUserStrategyBuilderTests
{
    [Fact]
    public void Build_SetNotificationTargetTypeWithDestination_ShouldCreateNotificationStrategy()
    {
        // Arrange
        var builder = new NotifyUserStrategyBuilder();

        // Act
        builder.SetNotificationTargetType(NotificationTarget.SingleUser);
        builder.SetDestination("");
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Build_Empty_ShouldFail()
    {
        // Arrange
        var builder = new NotifyUserStrategyBuilder();

        // Act
        // Assert
        Assert.Throws<ArgumentException>(builder.Build);
    }

    [Fact]
    public void Build_SetNotificationTargetTypeWithoutDestination_ShouldFail()
    {
        // Arrange
        var builder = new NotifyUserStrategyBuilder();

        // Act
        builder.SetNotificationTargetType(NotificationTarget.SingleUser);

        // Assert
        Assert.Throws<ArgumentException>(builder.Build);
    }
}
