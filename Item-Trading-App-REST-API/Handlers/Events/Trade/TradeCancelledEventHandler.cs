using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Resources.Events.Trades;
using Item_Trading_App_REST_API.Services.Notification;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Events.Trade;

public class TradeCancelledEventHandler : INotificationHandler<TradeCancelledEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public TradeCancelledEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(TradeCancelledEvent notification, CancellationToken cancellationToken)
    {
        return _clientNotificationService.SendUpdatedNotificationToUserAsync(
            notification.ReceiverId,
            NotificationCategoryTypes.Trade,
            notification.TradeId, new RespondedTradeNotification
            {
                Response = null
            });
    }
}
