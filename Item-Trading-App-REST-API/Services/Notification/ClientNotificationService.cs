using Item_Trading_App_Contracts.Notifications.Content;
using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Services.ConnectedUsers;
using System;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Notification;

public class ClientNotificationService : IClientNotificationService
{
    private readonly IConnectedUsersRepository _connectedUsersRepository;

    public ClientNotificationService(IConnectedUsersRepository connectedUsersRepository)
    {
        _connectedUsersRepository = connectedUsersRepository;
    }

    #region Create

    public Task SendCreatedNotificationToUserAsync(string userId, string categoryType, string id, object customData = null) =>
        _connectedUsersRepository.NotifyUserAsync(userId, CreateModifiedNotificationObject(NotificationTypes.Created, categoryType, id, customData));

    public Task SendCreatedNotificationToAllUsersAsync(string categoryType, string id, object customData = null) =>
        _connectedUsersRepository.NotifyUsersAsync(CreateModifiedNotificationObject(NotificationTypes.Created, categoryType, id, customData));

    public Task SendCreatedNotificationToUsersAsync(string[] userIds, string categoryType, string id, object customData = null) =>
        _connectedUsersRepository.NotifyUsersAsync(userIds, CreateModifiedNotificationObject(NotificationTypes.Created, categoryType, id, customData));

    public Task SendCreatedNotificationToAllUsersExceptAsync(string userId, string categoryType, string id, object customData = null) =>
        _connectedUsersRepository.NotifyAllUsersExceptAsync(userId, CreateModifiedNotificationObject(NotificationTypes.Created, categoryType, id, customData));

    #endregion Create

    #region Read

    public Task SendMessageNotificationToUserAsync(string userId, string content, DateTime dateTime) =>
        _connectedUsersRepository.NotifyUserAsync(userId, CreateMessageNotification(content, dateTime));

    public Task SendMessageNotificationToAllUsersAsync(string content, DateTime dateTime) =>
        _connectedUsersRepository.NotifyUsersAsync(CreateMessageNotification(content, dateTime));

    public Task SendMessageNotificationToUsersAsync(string[] userIds, string content, DateTime dateTime) =>
        _connectedUsersRepository.NotifyUsersAsync(userIds, CreateMessageNotification(content, dateTime));

    public Task SendMessageNotificationToAllUsersExceptAsync(string userId, string content, DateTime dateTime) =>
        _connectedUsersRepository.NotifyAllUsersExceptAsync(userId, CreateMessageNotification(content, dateTime));

    #endregion Read

    #region Update

    public Task SendUpdatedNotificationToUserAsync(string userId, string categoryType, string id, object customData = null) =>
        _connectedUsersRepository.NotifyUserAsync(userId, CreateModifiedNotificationObject(NotificationTypes.Changed, categoryType, id, customData));

    public Task SendUpdatedNotificationToAllUsersAsync(string categoryType, string id, object customData = null) =>
        _connectedUsersRepository.NotifyUsersAsync(CreateModifiedNotificationObject(NotificationTypes.Changed, categoryType, id, customData));

    public Task SendUpdatedNotificationToUsersAsync(string[] userIds, string categoryType, string id, object customData = null) =>
        _connectedUsersRepository.NotifyUsersAsync(userIds, CreateModifiedNotificationObject(NotificationTypes.Changed, categoryType, id, customData));

    public Task SendUpdatedNotificationToAllUsersExceptAsync(string userId, string categoryType, string id, object customData = null) =>
        _connectedUsersRepository.NotifyAllUsersExceptAsync(userId, CreateModifiedNotificationObject(NotificationTypes.Changed, categoryType, id, customData));

    #endregion Update

    #region Delete

    public Task SendDeletedNotificationToUserAsync(string userId, string categoryType, string id, object customData = null) =>
        _connectedUsersRepository.NotifyUserAsync(userId, CreateModifiedNotificationObject(NotificationTypes.Deleted, categoryType, id, customData));

    public Task SendDeletedNotificationToAllUsersAsync(string categoryType, string id, object customData = null) =>
        _connectedUsersRepository.NotifyUsersAsync(CreateModifiedNotificationObject(NotificationTypes.Deleted, categoryType, id, customData));

    public Task SendDeletedNotificationToUsersAsync(string[] userIds, string categoryType, string id, object customData = null) =>
        _connectedUsersRepository.NotifyUsersAsync(userIds, CreateModifiedNotificationObject(NotificationTypes.Deleted, categoryType, id, customData));

    public Task SendDeletedNotificationToAllUsersExceptAsync(string userId, string categoryType, string id, object customData = null) =>
        _connectedUsersRepository.NotifyAllUsersExceptAsync(userId, CreateModifiedNotificationObject(NotificationTypes.Deleted, categoryType, id, customData));

    #endregion Delete

    #region private

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

    private static object CreateModifiedNotificationObject(string notificationType, string categoryType, string id, object customData)
    {
        if (customData is null)
            return CreateModifiedNotification(notificationType, categoryType, id);
        else
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

    #endregion private
}
