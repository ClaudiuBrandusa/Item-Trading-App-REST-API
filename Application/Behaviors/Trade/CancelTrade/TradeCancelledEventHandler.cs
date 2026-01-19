using Application.Constants;
using Application.Helpers;
using Application.Models.Trades;
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
        var notificationStrategy = NotificationHelper.CreateSingleUserNotificationStrategy(notification.ReceiverId);

        return _clientNotificationService.SendUpdatedNotificationAsync(
            notificationStrategy,
            NotificationCategoryTypes.Trade,
            notification.TradeId, new RespondedTradeNotification
            {
                Response = null
            });
    }
}
