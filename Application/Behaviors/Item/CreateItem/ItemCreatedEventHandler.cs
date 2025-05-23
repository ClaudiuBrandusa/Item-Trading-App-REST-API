using Application.Constants;
using Application.Services.Notification;
using Application.Utils.Notifications;
using MediatR;

namespace Application.Behaviors.Item.CreateItem;

public class ItemCreatedEventHandler : INotificationHandler<ItemCreatedEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public ItemCreatedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(ItemCreatedEvent notification, CancellationToken cancellationToken)
    {
        return _clientNotificationService.SendCreatedNotificationAsync(
                NotificationHelper.CreateAllUsersExceptNotificationStrategy(notification.SenderUserId),
                NotificationCategoryTypes.Item,
                notification.Item.ItemId);
    }
}
