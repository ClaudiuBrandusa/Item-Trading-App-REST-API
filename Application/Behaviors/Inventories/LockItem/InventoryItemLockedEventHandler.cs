using Application.Constants;
using Application.Helpers;
using Application.Models.Inventories;
using Application.Services.Notification;
using Domain.DomainEvents.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.LockItem;

public class InventoryItemLockedEventHandler : INotificationHandler<InventoryItemLockedDomainEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public InventoryItemLockedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(InventoryItemLockedDomainEvent notification, CancellationToken cancellationToken)
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
