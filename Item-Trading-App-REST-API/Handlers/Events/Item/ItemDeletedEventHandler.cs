using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Resources.Events.Item;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Notification;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Events.Item;

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
            _clientNotificationService.SendDeletedNotificationToAllUsersExceptAsync(
                notification.UserId,
                NotificationCategoryTypes.Item,
                notification.ItemId)
        );
    }
}
