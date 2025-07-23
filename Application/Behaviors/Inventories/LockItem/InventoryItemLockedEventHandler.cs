using Application.Constants;
using Application.Models.Inventories;
using Application.Services.Notification;
using Application.Utils.Notifications;
using MediatR;

namespace Application.Behaviors.Inventories.LockItem;

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
