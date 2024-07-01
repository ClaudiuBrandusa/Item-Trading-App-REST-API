using Application.Constants;
using Application.Models.Inventory;
using Application.Services.Notification;
using MediatR;

namespace Application.Behaviors.Inventory.UnlockItem;

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
            return _clientNotificationService.SendUpdatedNotificationToUserAsync(
                notification.UserId,
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
