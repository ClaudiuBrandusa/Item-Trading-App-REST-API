using Application.Constants;
using Application.Models.Inventory;
using Application.Services.Notification;
using MediatR;

namespace Application.Behaviors.Inventory.LockItem;

public class InventoryItemLockedEventHandler : INotificationHandler<InventoryItemLockedEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public InventoryItemLockedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(InventoryItemLockedEvent notification, CancellationToken cancellationToken)
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
