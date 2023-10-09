using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Notification;

public interface INotificationService
{
    Task SendCreatedNotificationToUserAsync(string userId, string categoryType, string id, object customData = null);

    Task SendCreatedNotificationToAllUsersAsync(string categoryType, string id, object customData = null);

    Task SendCreatedNotificationToUsersAsync(List<string> userIds, string categoryType, string id, object customData = null);

    Task SendCreatedNotificationToAllUsersExceptAsync(string userId, string categoryType, string id, object customData = null);

    Task SendMessageNotificationToUserAsync(string userId, string content, DateTime dateTime);

    Task SendMessageNotificationToAllUsersAsync(string content, DateTime dateTime);

    Task SendMessageNotificationToUsersAsync(List<string> userIds, string content, DateTime dateTime);

    Task SendMessageNotificationToAllUsersExceptAsync(string userId, string content, DateTime dateTime);

    Task SendUpdatedNotificationToUserAsync(string userId, string categoryType, string id, object customData = null);

    Task SendUpdatedNotificationToAllUsersAsync(string categoryType, string id, object customData = null);

    Task SendUpdatedNotificationToUsersAsync(List<string> userIds, string categoryType, string id, object customData = null);

    Task SendUpdatedNotificationToAllUsersExceptAsync(string userId, string categoryType, string id, object customData = null);

    Task SendDeletedNotificationToUserAsync(string userId, string categoryType, string id, object customData = null);

    Task SendDeletedNotificationToAllUsersAsync(string categoryType, string id, object customData = null);

    Task SendDeletedNotificationToUsersAsync(List<string> userIds, string categoryType, string id, object customData = null);

    Task SendDeletedNotificationToAllUsersExceptAsync(string userId, string categoryType, string id, object customData = null);
}
