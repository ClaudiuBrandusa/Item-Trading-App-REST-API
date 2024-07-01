using Application.Constants;
using Application.Models.Trade;
using Application.Services.Notification;
using MediatR;

namespace Application.Behaviors.Trade.CancelTrade;

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
