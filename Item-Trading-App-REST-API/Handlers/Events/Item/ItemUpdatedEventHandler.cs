using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Resources.Events.Item;
using Item_Trading_App_REST_API.Services.Notification;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Events.Item;

public class ItemUpdatedEventHandler : INotificationHandler<ItemUpdatedEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public ItemUpdatedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(ItemUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return _clientNotificationService.SendUpdatedNotificationToAllUsersExceptAsync(
                notification.SenderUserId,
                NotificationCategoryTypes.Item,
                notification.Item.ItemId);
    }
}
