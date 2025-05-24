using Application.Models.Notifications;
using Application.Utils.Notifications;
using Application.Utils.Notifications.NotificationStrategies;
using Domain.Entities.Identity;

namespace Application_UnitTests.Notifications;

public class NotifyUserStrategyBuilderTests
{
    [Fact(DisplayName = "Attempt to build a notification strategy without setting anything in the builder -> Should throw and exception")]
    public void Build_AttemptToBuildWithoutAnyConfiguration_ShouldThrowAnException()
    {
        // Arrange

        var builder = new NotifyUserStrategyBuilder();

        // Act
        // Assert

        Assert.Throws<ArgumentException>(builder.Build);
    }

    [Fact(DisplayName = "Attempt to set the notification target type twice -> Should throw an exception")]
    public void SetNotificationTargetType_AttemptToSetNotificationTargetTwice_ShouldThrowAnException()
    {
        // Arrange

        var builder = new NotifyUserStrategyBuilder();
        var notificationTarget = NotificationTargetType.SingleUser;

        // Act

        builder.SetNotificationTargetType(notificationTarget);

        // Assert

        Assert.Throws<ArgumentException>(() => builder.SetNotificationTargetType(notificationTarget));
    }

    [Fact(DisplayName = "Attempt to set the notification destination as empty or null -> Should throw an exception")]
    public void SetDestination_AttemptToSetDestinationAsEmptyOrNull_ShouldThrowAnException()
    {
        // Arrange

        var builder = new NotifyUserStrategyBuilder();

        // Act
        // Assert

        Assert.Throws<ArgumentException>(() => builder.SetDestination(string.Empty));
        Assert.Throws<ArgumentException>(() => builder.SetDestination(null));
    }

    [Fact(DisplayName = "Attempt to set the notification destinations as a null or empty array -> Should throw an exception")]
    public void SetDestinations_AttemptToSetDestinationsAsEmptyOrNull_ShouldThrowAnException()
    {
        // Arrange

        var builder = new NotifyUserStrategyBuilder();

        // Act
        // Assert

        Assert.Throws<ArgumentException>(() => builder.SetDestinations(Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => builder.SetDestinations(null));
    }

    [Fact(DisplayName = "Build a single user notification strategy")]
    public void Build_BuildNotificationStrategyOfSingleUserType_ReturnsANotificationStrategyOfSingleUserType()
    {
        // Arrange

        var builder = new NotifyUserStrategyBuilder();

        // Act

        var notificationStrategy = builder
            .SetNotificationTargetType(NotificationTargetType.SingleUser)
            .SetDestination(User.GenerateId())
            .Build();

        // Assert

        Assert.NotNull(notificationStrategy);
        Assert.IsType<NotifySingleUserStrategy>(notificationStrategy);
    }

    [Fact(DisplayName = "Attempt to build a single user notification strategy without destination -> Should throw an exception")]
    public void Build_AttemptToBuildNotificationStrategyOfSingleUserTypeWithoutSettingDestination_ShouldThrowAnException()
    {
        // Arrange

        var builder = new NotifyUserStrategyBuilder();

        // Act

        builder
            .SetNotificationTargetType(NotificationTargetType.SingleUser);
            // .SetDestination(User.GenerateId())
        
        // Assert

        Assert.Throws<ArgumentException>(builder.Build);
    }

    [Fact(DisplayName = "Build a multiple users notification strategy")]
    public void Build_BuildNotificationStrategyOfMultipleUsersType_ReturnsANotificationStrategyOfMultipleUsersType()
    {
        // Arrange

        var builder = new NotifyUserStrategyBuilder();

        // Act

        var notificationStrategy = builder
            .SetNotificationTargetType(NotificationTargetType.MultipleUsers)
            .SetDestinations(new string[] { User.GenerateId(), User.GenerateId() })
            .Build();

        // Assert

        Assert.NotNull(notificationStrategy);
        Assert.IsType<NotifyMultipleUsersStrategy>(notificationStrategy);
    }

    [Fact(DisplayName = "Attempt to build a multiple users notification strategy without destinations -> Should throw an exception")]
    public void Build_AttemptToBuildNotificationStrategyOfMultipleUsersTypeWithoutSettingDestinations_ShouldThrowAnException()
    {
        // Arrange

        var builder = new NotifyUserStrategyBuilder();

        // Act

        builder
            .SetNotificationTargetType(NotificationTargetType.MultipleUsers);
            // .SetDestinations(new string[] { User.GenerateId(), User.GenerateId() })

        // Assert

        Assert.Throws<ArgumentException>(builder.Build);
    }

    [Fact(DisplayName = "Build an all users notification strategy")]
    public void Build_BuildNotificationStrategyOfAllUsersType_ReturnsANotificationStrategyOfAllUsersType()
    {
        // Arrange

        var builder = new NotifyUserStrategyBuilder();

        // Act

        var notificationStrategy = builder
            .SetNotificationTargetType(NotificationTargetType.AllUsers)
            .Build();

        // Assert

        Assert.NotNull(notificationStrategy);
        Assert.IsType<NotifyAllUsersStrategy>(notificationStrategy);
    }

    [Fact(DisplayName = "Build an all users except one user notification strategy")]
    public void Build_BuildNotificationStrategyOfAllUsersExceptType_ReturnsANotificationStrategyOfAllUsersExceptType()
    {
        // Arrange

        var builder = new NotifyUserStrategyBuilder();

        // Act

        var notificationStrategy = builder
            .SetNotificationTargetType(NotificationTargetType.AllExcept)
            .SetDestination(User.GenerateId())
            .Build();

        // Assert

        Assert.NotNull(notificationStrategy);
        Assert.IsType<NotifyAllUsersExceptStrategy>(notificationStrategy);
    }

    [Fact(DisplayName = "Attempt to build an all users except notification strategy without destination -> Should throw an exception")]
    public void Build_AttemptToBuildNotificationStrategyOfAllUsersExceptTypeWithoutSettingDestination_ShouldThrowAnException()
    {
        // Arrange

        var builder = new NotifyUserStrategyBuilder();

        // Act

        builder
            .SetNotificationTargetType(NotificationTargetType.AllExcept);
            // .SetDestination(User.GenerateId())

        // Assert

        Assert.Throws<ArgumentException>(builder.Build);
    }
}
