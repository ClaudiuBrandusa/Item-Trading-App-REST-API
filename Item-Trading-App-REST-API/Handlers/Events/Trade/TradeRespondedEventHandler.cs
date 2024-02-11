using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Resources.Events.Trades;
using Item_Trading_App_REST_API.Services.Notification;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Events.Trade;

public class TradeRespondedEventHandler : INotificationHandler<TradeRespondedEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public TradeRespondedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(TradeRespondedEvent notification, CancellationToken cancellationToken)
    {
        return _clientNotificationService.SendUpdatedNotificationToUserAsync(
            notification.SenderId,
            NotificationCategoryTypes.Trade,
            notification.TradeId,
            new RespondedTradeNotification
            {
                Response = notification.Response
            });
    }
}
