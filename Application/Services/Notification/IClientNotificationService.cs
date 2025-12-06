using Application.Utils.Notifications;

namespace Application.Services.Notification;

public interface IClientNotificationService
{
    Task SendCreatedNotificationAsync(INotifyUserStrategy nus, string categoryType, string id, object? customData = null);

    Task SendMessageNotificationAsync(INotifyUserStrategy nus, string content, DateTime dateTime);

    Task SendUpdatedNotificationAsync(INotifyUserStrategy nus, string categoryType, string id, object? customData = null);
    
    Task SendDeletedNotificationAsync(INotifyUserStrategy nus, string categoryType, string id, object? customData = null);
}
