using Item_Trading_App_Contracts.Notifications.Content;
using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Services.ConnectedUsers;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Services.Notification;

public class NotificationService : INotificationService
{
    private readonly IConnectedUsersRepository _connectedUsersRepository;

    public NotificationService(IConnectedUsersRepository connectedUsersRepository)
    {
        _connectedUsersRepository = connectedUsersRepository;
    }

    #region Create

    public async Task SendCreatedNotificationToUserAsync(string userId, string categoryType, string id, object customData = null)
    {
        await _connectedUsersRepository.NotifyUserAsync(userId, CreateModifiedNotificationObject(NotificationTypes.Created, categoryType, id, customData));
    }

    public async Task SendCreatedNotificationToAllUsersAsync(string categoryType, string id, object customData = null)
    {
        await _connectedUsersRepository.NotifyUsersAsync(CreateModifiedNotificationObject(NotificationTypes.Created, categoryType, id, customData));
    }

    public async Task SendCreatedNotificationToUsersAsync(List<string> userIds, string categoryType, string id, object customData = null)
    {
        await _connectedUsersRepository.NotifyUsersAsync(userIds, CreateModifiedNotificationObject(NotificationTypes.Created, categoryType, id, customData));
    }

    public async Task SendCreatedNotificationToAllUsersExceptAsync(string userId, string categoryType, string id, object customData = null)
    {
        await _connectedUsersRepository.NotifyAllUsersExceptAsync(userId, CreateModifiedNotificationObject(NotificationTypes.Created, categoryType, id, customData));
    }

    #endregion Create

    #region Read

    public async Task SendMessageNotificationToUserAsync(string userId, string content, DateTime dateTime)
    {
        await _connectedUsersRepository.NotifyUserAsync(userId, CreateMessageNotification(content, dateTime));
    }

    public async Task SendMessageNotificationToAllUsersAsync(string content, DateTime dateTime)
    {
        await _connectedUsersRepository.NotifyUsersAsync(CreateMessageNotification(content, dateTime));
    }

    public async Task SendMessageNotificationToUsersAsync(List<string> userIds, string content, DateTime dateTime)
    {
        await _connectedUsersRepository.NotifyUsersAsync(userIds, CreateMessageNotification(content, dateTime));
    }

    public async Task SendMessageNotificationToAllUsersExceptAsync(string userId, string content, DateTime dateTime)
    {
        await _connectedUsersRepository.NotifyAllUsersExceptAsync(userId, CreateMessageNotification(content, dateTime));
    }

    #endregion Read

    #region Update

    public async Task SendUpdatedNotificationToUserAsync(string userId, string categoryType, string id, object customData = null)
    {
        await _connectedUsersRepository.NotifyUserAsync(userId, CreateModifiedNotificationObject(NotificationTypes.Changed, categoryType, id, customData));
    }

    public async Task SendUpdatedNotificationToAllUsersAsync(string categoryType, string id, object customData = null)
    {
        await _connectedUsersRepository.NotifyUsersAsync(CreateModifiedNotificationObject(NotificationTypes.Changed, categoryType, id, customData));
    }

    public async Task SendUpdatedNotificationToUsersAsync(List<string> userIds, string categoryType, string id, object customData = null)
    {
        await _connectedUsersRepository.NotifyUsersAsync(userIds, CreateModifiedNotificationObject(NotificationTypes.Changed, categoryType, id, customData));
    }

    public async Task SendUpdatedNotificationToAllUsersExceptAsync(string userId, string categoryType, string id, object customData = null)
    {
        await _connectedUsersRepository.NotifyAllUsersExceptAsync(userId, CreateModifiedNotificationObject(NotificationTypes.Changed, categoryType, id, customData));
    }

    #endregion Update

    #region Delete

    public async Task SendDeletedNotificationToUserAsync(string userId, string categoryType, string id, object customData = null)
    {
        await _connectedUsersRepository.NotifyUserAsync(userId, CreateModifiedNotificationObject(NotificationTypes.Deleted, categoryType, id, customData));
    }

    public async Task SendDeletedNotificationToAllUsersAsync(string categoryType, string id, object customData = null)
    {
        await _connectedUsersRepository.NotifyUsersAsync(CreateModifiedNotificationObject(NotificationTypes.Deleted, categoryType, id, customData));
    }

    public async Task SendDeletedNotificationToUsersAsync(List<string> userIds, string categoryType, string id, object customData = null)
    {
        await _connectedUsersRepository.NotifyUsersAsync(userIds, CreateModifiedNotificationObject(NotificationTypes.Deleted, categoryType, id, customData));
    }

    public async Task SendDeletedNotificationToAllUsersExceptAsync(string userId, string categoryType, string id, object customData = null)
    {
        await _connectedUsersRepository.NotifyAllUsersExceptAsync(userId, CreateModifiedNotificationObject(NotificationTypes.Deleted, categoryType, id, customData));
    }

    #endregion Delete

    #region private

    private Notification<MessageContent> CreateMessageNotification(string content, DateTime dateTime)
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

    private object CreateModifiedNotificationObject(string notificationType, string categoryType, string id, object customData)
    {
        if (customData is null)
        {
            return CreateModifiedNotification(notificationType, categoryType, id);
        } else
        {
            return CreateModifiedNotification(notificationType, categoryType, id, customData);
        }
    }

    private Notification<ModifiedContent> CreateModifiedNotification(string notificationType, string categoryType, string id)
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

    private Notification<ModifiedContentWithCustomData> CreateModifiedNotification(string notificationType, string categoryType, string id, object customData)
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

    #endregion private
}
