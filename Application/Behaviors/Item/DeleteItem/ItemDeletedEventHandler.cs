using Application.Behaviors.Inventories.ListUsersOwningItem;
using Application.Behaviors.Inventories.RemoveItemFromUsers;
using Application.Constants;
using Application.Services.Notification;
using Application.Utils.Notifications;
using MediatR;

namespace Application.Behaviors.Item.DeleteItem;

public class ItemDeletedEventHandler : INotificationHandler<ItemDeletedEvent>
{
    private readonly IClientNotificationService _clientNotificationService;
    private readonly IMediator _mediator;

    public ItemDeletedEventHandler(IClientNotificationService clientNotificationService, IMediator mediator)
    {
        _clientNotificationService = clientNotificationService;
        _mediator = mediator;
    }

    public Task Handle(ItemDeletedEvent notification, CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            Task.Run(async () =>
            {
                var usersOwningTheItem = await _mediator.Send(new GetUserIdsOwningItemQuery { ItemId = notification.ItemId });
                await _mediator.Send(new RemoveItemFromUsersCommand { ItemId = notification.ItemId, UserIds = usersOwningTheItem.UserIds });
            }, cancellationToken),
            _clientNotificationService.SendDeletedNotificationAsync(
                NotificationHelper.CreateAllUsersExceptNotificationStrategy(notification.UserId),
                NotificationCategoryTypes.Item,
                notification.ItemId)
        );
    }
}
