using Application.Services.Notification;
using Application.Constants;
using Item_Trading_App_Contracts.Notifications.Content;
using Item_Trading_App_Contracts.Notifications;
using Application.Services.ConnectedUsers;
using Application.Utils.Notifications;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace Infrastructure.Services.Notification;

public class ClientNotificationService : IClientNotificationService
{
    private readonly IConnectedUsersRepository _connectedUsersRepository;

    public ClientNotificationService(IConnectedUsersRepository connectedUsersRepository)
    {
        _connectedUsersRepository = connectedUsersRepository;
    }

    public Task SendCreatedNotificationAsync(INotifyUserStrategy nus, string categoryType, string id, object? customData = null)
    {
        var notification = CreateModifiedNotification(NotificationTypes.Created, categoryType, id, customData);

        return nus.Notify(notification, _connectedUsersRepository);
    }

    public Task SendMessageNotificationAsync(INotifyUserStrategy nus, string content, DateTime dateTime)
    {
        var notification = CreateMessageNotification(content, dateTime);

        return nus.Notify(notification, _connectedUsersRepository);
    }

    public Task SendUpdatedNotificationAsync(INotifyUserStrategy nus, string categoryType, string id, object? customData = null)
    {
        var notification = CreateModifiedNotification(NotificationTypes.Changed, categoryType, id, customData);

        return nus.Notify(notification, _connectedUsersRepository);
    }

    public Task SendDeletedNotificationAsync(INotifyUserStrategy nus, string categoryType, string id, object? customData = null)
    {
        var notification = CreateModifiedNotification(NotificationTypes.Deleted, categoryType, id, customData);

        return nus.Notify(notification, _connectedUsersRepository);
    }

    private static Notification<MessageContent> CreateMessageNotification(string content, DateTime dateTime)
    {
        return new Notification<MessageContent>
        {
            Type = NotificationTypes.Information,
            Content = new MessageContent
            {
                Content = content,
                CreatedDateTime = dateTime
            }
        };
    }

    private static IClientNotification CreateModifiedNotification(string notificationType, string categoryType, string id, object? customData)
    {
        if (customData is not null)
        {
            var jsonNode = JsonNode.Parse(JsonSerializer.Serialize(customData));

            if (jsonNode is not null)
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

        return new Notification<ModifiedContent>
        {
            Type = notificationType,
            Content = new ModifiedContent
            {
                Category = categoryType,
                Id = id
            }
        };
    }
}
