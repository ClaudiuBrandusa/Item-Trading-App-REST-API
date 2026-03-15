using Application.Constants;
using Application.Helpers;
using Application.Models.Inventories;
using Application.Services.Notification;
using Domain.DomainEvents.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.DropItem;

public class InventoryItemDroppedEventHandler : INotificationHandler<InventoryItemDroppedDomainEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public InventoryItemDroppedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(InventoryItemDroppedDomainEvent notification, CancellationToken cancellationToken)
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
