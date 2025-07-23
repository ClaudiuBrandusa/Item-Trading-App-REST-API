using Application.Constants;
using Application.Models.Inventories;
using Application.Services.Notification;
using Application.Utils.Notifications;
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
        if (notification.Notify)
            return _clientNotificationService.SendUpdatedNotificationAsync(
                NotificationHelper.CreateSingleUserNotificationStrategy(notification.UserId),
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
