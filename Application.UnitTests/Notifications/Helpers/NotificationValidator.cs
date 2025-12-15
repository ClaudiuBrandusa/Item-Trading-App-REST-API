using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_Contracts.Notifications.Content;

namespace Application_UnitTests.Notifications.Helpers;

public class NotificationValidator
{
    private string expectedNotificationType = string.Empty;
    private string expectedCategory = string.Empty;
    private string expectedId = string.Empty;
    private object expectedCustomData = null;
    private object expectedContent = null;
    private DateTime expectedDateTime = DateTime.MinValue;

    public NotificationValidator HasNotificationType(string expectedNotificationType)
    {
        this.expectedNotificationType = expectedNotificationType;
        return this;
    }

    public NotificationValidator HasContent(string expectedCategory, string expectedId, object expectedCustomData = null)
    {
        this.expectedCategory = expectedCategory;
        this.expectedId = expectedId;
        this.expectedCustomData = expectedCustomData;
        return this;
    }

    public NotificationValidator HasMessageContent(object expectedContent, DateTime expectedDateTime)
    {
        this.expectedContent = expectedContent;
        this.expectedDateTime = expectedDateTime;
        return this;
    }

    public bool Validate(Notification<ModifiedContentWithCustomData> notification)
    {
        return notification.Type == expectedNotificationType &&
            notification.Content.Category == expectedCategory &&
            notification.Content.Id == expectedId &&
            notification.Content.CustomData == expectedCustomData;
    }

    public bool Validate(Notification<MessageContent> notification)
    {
        return notification.Type == expectedNotificationType &&
            notification.Content.Content == expectedContent &&
            notification.Content.CreatedDateTime == expectedDateTime;
    }
}
