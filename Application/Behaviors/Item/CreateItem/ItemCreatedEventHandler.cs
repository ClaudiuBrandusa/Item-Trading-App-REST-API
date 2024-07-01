using Application.Constants;
using Application.Services.Notification;
using MediatR;

namespace Application.Behaviors.Item.CreateItem;

public class ItemCreatedEventHandler : INotificationHandler<ItemCreatedEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public ItemCreatedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(ItemCreatedEvent notification, CancellationToken cancellationToken)
    {
        return _clientNotificationService.SendCreatedNotificationToAllUsersExceptAsync(
                notification.SenderUserId,
                NotificationCategoryTypes.Item,
                notification.Item.ItemId);
    }
}
