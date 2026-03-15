using Application.Behaviors.Inventories.ListUsersOwningItem;
using Application.Behaviors.Inventories.RemoveItemFromUsers;
using Application.Constants;
using Application.Helpers;
using Application.Services.Notification;
using Domain.DomainEvents.Items;
using MediatR;

namespace Application.Behaviors.Item.DeleteItem;

public class ItemDeletedEventHandler : INotificationHandler<ItemDeletedDomainEvent>
{
    private readonly IClientNotificationService _clientNotificationService;
    private readonly IMediator _mediator;

    public ItemDeletedEventHandler(IClientNotificationService clientNotificationService, IMediator mediator)
    {
        _clientNotificationService = clientNotificationService;
        _mediator = mediator;
    }

    public Task Handle(ItemDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        var notificationStrategy = NotificationHelper.CreateAllUsersExceptNotificationStrategy(notification.UserId);

        return Task.WhenAll(
            Task.Run(async () =>
            {
                var usersOwningTheItem = await _mediator.Send(new GetUserIdsOwningItemQuery { ItemId = notification.ItemId });
                await _mediator.Send(new RemoveItemFromUsersCommand { ItemId = notification.ItemId, UserIds = usersOwningTheItem.UserIds });
            }, cancellationToken),
            _clientNotificationService.SendDeletedNotificationAsync(
                notificationStrategy,
                NotificationCategoryTypes.Item,
                notification.ItemId)
        );
    }
}
