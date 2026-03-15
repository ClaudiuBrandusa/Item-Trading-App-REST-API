using Application.Behaviors.Identity.GetUsername;
using Application.Constants;
using Application.Helpers;
using Application.Services.Notification;
using Domain.DomainEvents.Trades;
using MediatR;

namespace Application.Behaviors.Trade.CreateTrade;

public class TradeCreatedDomainEventHandler : INotificationHandler<TradeCreatedDomainEvent>
{
    private readonly IClientNotificationService _clientNotificationService;
    private readonly IMediator _mediator;

    public TradeCreatedDomainEventHandler(IClientNotificationService clientNotificationService, IMediator mediator)
    {
        _clientNotificationService = clientNotificationService;
        _mediator = mediator;
    }

    public Task Handle(TradeCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var notificationStrategy = NotificationHelper.CreateSingleUserNotificationStrategy(notification.ReceiverId);
        
        return Task.WhenAll(
            _clientNotificationService.SendCreatedNotificationAsync(
                notificationStrategy,
                NotificationCategoryTypes.Trade,
                notification.TradeId),
            Task.Run(async () =>
            {
                var username = await _mediator.Send(new GetUsernameQuery { UserId = notification.ReceiverId });
                await _clientNotificationService.SendMessageNotificationAsync(
                        notificationStrategy,
                        $"You've received a trade from {username}",
                        DateTime.UtcNow);
            }, CancellationToken.None));
    }
}
