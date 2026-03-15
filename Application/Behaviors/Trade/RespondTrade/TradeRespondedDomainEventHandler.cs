using Application.Constants;
using Application.Helpers;
using Application.Models.Trades;
using Application.Services.Notification;
using Domain.DomainEvents.Trades;
using MediatR;

namespace Application.Behaviors.Trade.RespondTrade;

public class TradeRespondedDomainEventHandler : INotificationHandler<TradeRespondedDomainEvent>
{
    private readonly IClientNotificationService _clientNotificationService;

    public TradeRespondedDomainEventHandler(IClientNotificationService clientNotificationService)
    {
        _clientNotificationService = clientNotificationService;
    }

    public Task Handle(TradeRespondedDomainEvent notification, CancellationToken cancellationToken)
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
