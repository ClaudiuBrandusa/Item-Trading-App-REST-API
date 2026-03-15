using Application.Constants;
using Application.Helpers;
using Application.Models.Inventories;
using Application.Services.Notification;
using Domain.DomainEvents.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.AddItem;

public class InventoryItemAddedEventHandler : INotificationHandler<InventoryItemAddedDomainEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public InventoryItemAddedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(InventoryItemAddedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Notify)
            return _clientNotificationService.SendUpdatedNotificationAsync(
                NotificationHelper.CreateSingleUserNotificationStrategy(notification.UserId),
                NotificationCategoryTypes.Inventory,
                notification.ItemId,
                new InventoryItemQuantityNotification { AddAmount = true, Amount = notification.Quantity });

        return Task.CompletedTask;
    }
}
