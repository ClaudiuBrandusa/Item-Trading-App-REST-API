using Application.Constants;
using Application.Helpers;
using Application.Services.Notification;
using Domain.DomainEvents.Items;
using MediatR;

namespace Application.Behaviors.Item.CreateItem;

public class ItemCreatedEventHandler : INotificationHandler<ItemCreatedDomainEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public ItemCreatedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(ItemCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var notificationStrategy = NotificationHelper.CreateAllUsersExceptNotificationStrategy(notification.SenderUserId);

        return _clientNotificationService.SendCreatedNotificationAsync(
                notificationStrategy,
                NotificationCategoryTypes.Item,
                notification.Item.ItemId);
    }
}
