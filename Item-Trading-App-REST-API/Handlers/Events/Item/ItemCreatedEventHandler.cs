using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Resources.Events.Item;
using Item_Trading_App_REST_API.Services.Notification;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Events.Item;

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
