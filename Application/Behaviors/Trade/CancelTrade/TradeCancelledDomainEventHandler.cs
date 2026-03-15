using Application.Constants;
using Application.Helpers;
using Application.Models.Trades;
using Application.Services.Notification;
using Domain.DomainEvents.Trades;
using MediatR;

namespace Application.Behaviors.Trade.CancelTrade;

public class TradeCancelledDomainEventHandler : INotificationHandler<TradeCancelledDomainEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public TradeCancelledDomainEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(TradeCancelledDomainEvent notification, CancellationToken cancellationToken)
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
