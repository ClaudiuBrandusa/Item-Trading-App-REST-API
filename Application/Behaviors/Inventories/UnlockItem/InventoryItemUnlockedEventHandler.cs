using Application.Constants;
using Application.Helpers;
using Application.Models.Inventories;
using Application.Services.Notification;
using MediatR;

namespace Application.Behaviors.Inventories.UnlockItem;

public class InventoryItemUnlockedEventHandler : INotificationHandler<InventoryItemUnlockedEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public InventoryItemUnlockedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(InventoryItemUnlockedEvent notification, CancellationToken cancellationToken)
    {
        var notificationStrategy = NotificationHelper.CreateSingleUserNotificationStrategy(notification.UserId);

        if (notification.Notify)
            return _clientNotificationService.SendUpdatedNotificationAsync(
                notificationStrategy,
                NotificationCategoryTypes.Inventory,
                notification.ItemId,
                new InventoryItemQuantityNotification
                {
                    AddAmount = true,
                    Amount = notification.Quantity
                });

        return Task.CompletedTask;
    }
}
