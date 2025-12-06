using Application.Constants;
using Application.Helpers;
using Application.Models.Inventory;
using Application.Services.Notification;
using MediatR;

namespace Application.Behaviors.Inventory.DropItem;

public class InventoryItemDroppedEventHandler : INotificationHandler<InventoryItemDroppedEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public InventoryItemDroppedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(InventoryItemDroppedEvent notification, CancellationToken cancellationToken)
    {
        var notificationStrategy = NotificationHelper.CreateSingleUserNotificationStrategy(notification.UserId);

        if (notification.Notify)
            return _clientNotificationService.SendUpdatedNotificationAsync(
                notificationStrategy,
                NotificationCategoryTypes.Inventory,
                notification.ItemId,
                new InventoryItemQuantityNotification
                {
                    AddAmount = false,
                    Amount = notification.Quantity
                });

        return Task.CompletedTask;
    }
}
