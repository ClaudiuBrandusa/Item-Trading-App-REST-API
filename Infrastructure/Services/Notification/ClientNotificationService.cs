using Application.Services.Notification;
using Application.Constants;
using Item_Trading_App_Contracts.Notifications.Content;
using Item_Trading_App_Contracts.Notifications;
using Application.Services.ConnectedUsers;
using Application.Utils.Notifications;

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
        var notification = CreateModifiedNotificationObject(NotificationTypes.Created, categoryType, id, customData);

        return nus.Notify(notification, _connectedUsersRepository);
    }

    public Task SendMessageNotificationAsync(INotifyUserStrategy nus, string content, DateTime dateTime)
    {
        var notification = CreateMessageNotification(content, dateTime);

        return nus.Notify(notification, _connectedUsersRepository);
    }

    public Task SendUpdatedNotificationAsync(INotifyUserStrategy nus, string categoryType, string id, object? customData = null)
    {
        var notification = CreateModifiedNotificationObject(NotificationTypes.Changed, categoryType, id, customData);

        return nus.Notify(notification, _connectedUsersRepository);
    }

    public Task SendDeletedNotificationAsync(INotifyUserStrategy nus, string categoryType, string id, object? customData = null)
    {
        var notification = CreateModifiedNotificationObject(NotificationTypes.Deleted, categoryType, id, customData);

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

    private static Notification<ModifiedContent> CreateModifiedNotificationObject(string notificationType, string categoryType, string id)
    {
        return CreateModifiedNotification(notificationType, categoryType, id);
    }

    private static Notification<ModifiedContentWithCustomData> CreateModifiedNotificationObject(string notificationType, string categoryType, string id, object customData)
    {
        return CreateModifiedNotification(notificationType, categoryType, id, customData);
    }

    private static Notification<ModifiedContent> CreateModifiedNotification(string notificationType, string categoryType, string id)
    {
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

    private static Notification<ModifiedContentWithCustomData> CreateModifiedNotification(string notificationType, string categoryType, string id, object customData)
    {
        return new Notification<ModifiedContentWithCustomData>
        {
            Type = notificationType,
            Content = new ModifiedContentWithCustomData
            {
                Category = categoryType,
                Id = id,
                CustomData = customData
            }
        };
    }
}
