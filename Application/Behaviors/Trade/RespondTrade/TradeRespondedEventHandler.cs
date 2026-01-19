using Application.Constants;
using Application.Helpers;
using Application.Models.Trades;
using Application.Services.Notification;
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
        var notificationStrategy = NotificationHelper.CreateSingleUserNotificationStrategy(notification.SenderId);

        return _clientNotificationService.SendUpdatedNotificationAsync(
            notificationStrategy,
            NotificationCategoryTypes.Trade,
            notification.TradeId,
            new RespondedTradeNotification
            {
                Response = notification.Response
            });
    }
}
