﻿using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Resources.Events.Inventory;
using Item_Trading_App_REST_API.Services.Notification;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Events.Inventory;

public class InventoryItemAddedEventHandler : INotificationHandler<InventoryItemAddedEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public InventoryItemAddedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(InventoryItemAddedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Notify)
            return _clientNotificationService.SendUpdatedNotificationToUserAsync(
                notification.UserId,
                NotificationCategoryTypes.Inventory,
                notification.ItemId,
                new InventoryItemQuantityNotification { AddAmount = true, Amount = notification.Quantity });

        return Task.CompletedTask;
    }
}