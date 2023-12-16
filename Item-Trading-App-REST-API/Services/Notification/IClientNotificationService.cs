using System;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Notification;

public interface IClientNotificationService
{
    Task SendCreatedNotificationToUserAsync(string userId, string categoryType, string id, object customData = null);

    Task SendCreatedNotificationToAllUsersAsync(string categoryType, string id, object customData = null);

    Task SendCreatedNotificationToUsersAsync(string[] userIds, string categoryType, string id, object customData = null);

    Task SendCreatedNotificationToAllUsersExceptAsync(string userId, string categoryType, string id, object customData = null);

    Task SendMessageNotificationToUserAsync(string userId, string content, DateTime dateTime);

    Task SendMessageNotificationToAllUsersAsync(string content, DateTime dateTime);

    Task SendMessageNotificationToUsersAsync(string[] userIds, string content, DateTime dateTime);

    Task SendMessageNotificationToAllUsersExceptAsync(string userId, string content, DateTime dateTime);

    Task SendUpdatedNotificationToUserAsync(string userId, string categoryType, string id, object customData = null);

    Task SendUpdatedNotificationToAllUsersAsync(string categoryType, string id, object customData = null);

    Task SendUpdatedNotificationToUsersAsync(string[] userIds, string categoryType, string id, object customData = null);

    Task SendUpdatedNotificationToAllUsersExceptAsync(string userId, string categoryType, string id, object customData = null);

    Task SendDeletedNotificationToUserAsync(string userId, string categoryType, string id, object customData = null);

    Task SendDeletedNotificationToAllUsersAsync(string categoryType, string id, object customData = null);

    Task SendDeletedNotificationToUsersAsync(string[] userIds, string categoryType, string id, object customData = null);

    Task SendDeletedNotificationToAllUsersExceptAsync(string userId, string categoryType, string id, object customData = null);
}
