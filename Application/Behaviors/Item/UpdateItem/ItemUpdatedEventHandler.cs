using Application.Constants;
using Application.Services.Notification;
using Application.Utils.Notifications;
using MediatR;

namespace Application.Behaviors.Item.UpdateItem;

public class ItemUpdatedEventHandler : INotificationHandler<ItemUpdatedEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public ItemUpdatedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(ItemUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return _clientNotificationService.SendUpdatedNotificationAsync(
                NotificationHelper.CreateAllUsersExceptNotificationStrategy(notification.SenderUserId),
                NotificationCategoryTypes.Item,
                notification.Item.ItemId);
    }
}
