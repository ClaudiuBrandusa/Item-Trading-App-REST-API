using System.Text.Json;
using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_Contracts.Notifications.Content;

namespace Application_UnitTests.Notifications.Helpers;

public class NotificationValidator<T>
{
    private string expectedNotificationType = string.Empty;
    private string expectedCategory = string.Empty;
    private string expectedId = string.Empty;
    private object? expectedContent;
    private Func<T?, bool>? customValidationRule;
    private DateTime expectedDateTime = DateTime.MinValue;

    public NotificationValidator<T> HasNotificationType(string expectedNotificationType)
    {
        this.expectedNotificationType = expectedNotificationType;
        return this;
    }

    public NotificationValidator<T> HasContent(string expectedCategory, string expectedId)
    {
        this.expectedCategory = expectedCategory;
        this.expectedId = expectedId;
        return this;
    }

    public NotificationValidator<T> HasMessageContent(object expectedContent, DateTime expectedDateTime)
    {
        this.expectedContent = expectedContent;
        this.expectedDateTime = expectedDateTime;
        return this;
    }

    public NotificationValidator<T> HasCustomDataValidationRule(Func<T?, bool> customValidationRule)
    {
        this.customValidationRule = customValidationRule;
        return this;
    }

    public bool Validate(Notification<ModifiedContent> notification)
    {
        return notification.Type == expectedNotificationType &&
            notification.Content.Category == expectedCategory &&
            notification.Content.Id == expectedId;
    }

    public bool Validate(Notification<ModifiedContentJson> notification)
    {
        return notification.Type == expectedNotificationType &&
            notification.Content.Category == expectedCategory &&
            notification.Content.Id == expectedId && 
            (
                customValidationRule is null ||
                customValidationRule(notification.Content.Content.Deserialize<T>())
            );
    }

    public bool Validate(Notification<MessageContent> notification)
    {
        return notification.Type == expectedNotificationType &&
            notification.Content.Content == expectedContent &&
            notification.Content.CreatedDateTime == expectedDateTime;
    }
}
