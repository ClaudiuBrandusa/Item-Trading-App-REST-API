using Application.Constants;
using Application.Helpers;
using Application.Services.Notification;
using Domain.DomainEvents.Items;
using MediatR;

namespace Application.Behaviors.Item.UpdateItem;

public class ItemUpdatedEventHandler : INotificationHandler<ItemUpdatedDomainEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public ItemUpdatedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(ItemUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var notificationStrategy = NotificationHelper.CreateAllUsersExceptNotificationStrategy(notification.SenderUserId);

        return _clientNotificationService.SendUpdatedNotificationAsync(
                notificationStrategy,
                NotificationCategoryTypes.Item,
                notification.Item.ItemId);
    }
}
