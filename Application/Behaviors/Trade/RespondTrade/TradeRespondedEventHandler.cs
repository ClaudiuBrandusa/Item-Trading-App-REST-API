using Application.Constants;
using Application.Models.Trades;
using Application.Services.Notification;
using Application.Utils.Notifications;
using MediatR;

namespace Application.Behaviors.Trade.RespondTrade;

public class TradeRespondedEventHandler : INotificationHandler<TradeRespondedEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public TradeRespondedEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(TradeRespondedEvent notification, CancellationToken cancellationToken)
    {
        return _clientNotificationService.SendUpdatedNotificationAsync(
            NotificationHelper.CreateSingleUserNotificationStrategy(notification.SenderId),
            NotificationCategoryTypes.Trade,
            notification.TradeId,
            new RespondedTradeNotification
            {
                Response = notification.Response
            });
    }
}
