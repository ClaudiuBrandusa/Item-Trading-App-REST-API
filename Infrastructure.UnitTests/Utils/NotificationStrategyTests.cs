using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Constants;
using Application.Helpers;
using Application.Services.ConnectedUsers;
using Application.Utils.Notifications;
using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_Contracts.Notifications.Content;
using Moq;

namespace Infrastructure_UnitTests.Utils;

public class NotificationStrategyTests
{
    [Fact]
    public async Task NotifyAllUsers()
    {
        var connectedUsersRepositoryMock = new Mock<IConnectedUsersRepository>();
        connectedUsersRepositoryMock.Setup(x => x.NotifyUsersAsync(It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        INotifyUserStrategy notifyAllUsersStrategy = NotificationHelper.CreateAllUsersNotificationStrategy();
        string categoryType = "SomeCategory";
        string id = "12345";
        var customData = new { Info = "Additional info" };

        var notification = CreateModifiedNotificationObject(NotificationTypes.Changed, categoryType, id, customData);

        await notifyAllUsersStrategy.Notify(notification, connectedUsersRepositoryMock.Object);

        connectedUsersRepositoryMock.Verify(x => x.NotifyUsersAsync(notification), Times.Once);
    }

    private static Notification<ModifiedContentJson> CreateModifiedNotificationObject(string notificationType, string categoryType, string id, object customData)
    {
        return CreateModifiedNotification(notificationType, categoryType, id, customData);
    }

    private static Notification<ModifiedContentJson> CreateModifiedNotification(string notificationType, string categoryType, string id, object customData)
    {
        var jsonNode = JsonNode.Parse(JsonSerializer.Serialize(customData));

        return new Notification<ModifiedContentJson>
        {
            Type = notificationType,
            Content = new ModifiedContentJson
            {
                Category = categoryType,
                Id = id,
                Content = jsonNode
            }
        };
    }
}
