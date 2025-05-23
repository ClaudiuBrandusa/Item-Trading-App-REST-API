using Application.Constants;
using Application.Models.Trades;
using Application.Services.Notification;
using Application.Utils.Notifications;
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
        return _clientNotificationService.SendUpdatedNotificationAsync(
            NotificationHelper.CreateSingleUserNotificationStrategy(notification.ReceiverId),
            NotificationCategoryTypes.Trade,
            notification.TradeId, new RespondedTradeNotification
            {
                Response = null
            });
    }
}
