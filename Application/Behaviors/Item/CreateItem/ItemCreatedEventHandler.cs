using Application.Constants;
using Application.Helpers;
using Application.Services.Notification;
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
        var notificationStrategy = NotificationHelper.CreateAllUsersExceptNotificationStrategy(notification.SenderUserId);

        return _clientNotificationService.SendCreatedNotificationAsync(
                notificationStrategy,
                NotificationCategoryTypes.Item,
                notification.Item.ItemId);
    }
}
