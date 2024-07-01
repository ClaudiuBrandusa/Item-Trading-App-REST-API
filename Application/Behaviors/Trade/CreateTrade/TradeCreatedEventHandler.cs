using Application.Behaviors.Identity.GetUsername;
using Application.Constants;
using Application.Services.Notification;
using MediatR;

namespace Application.Behaviors.Trade.CreateTrade;

public class TradeCreatedEventHandler : INotificationHandler<TradeCreatedEvent>
{
    private readonly IClientNotificationService _clientNotificationService;
    private readonly IMediator _mediator;

    public TradeCreatedEventHandler(IClientNotificationService clientNotificationService, IMediator mediator)
    {
        _clientNotificationService = clientNotificationService;
        _mediator = mediator;
    }

    public Task Handle(TradeCreatedEvent notification, CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            _clientNotificationService.SendCreatedNotificationToUserAsync(
                notification.ReceiverId,
                NotificationCategoryTypes.Trade,
                notification.TradeId),
            Task.Run(async () =>
            {
                var username = await _mediator.Send(new GetUsernameQuery { UserId = notification.ReceiverId });
                await _clientNotificationService.SendMessageNotificationToUserAsync(
                        notification.ReceiverId,
                        $"You've received a trade from {username}",
                        DateTime.Now);
            }, CancellationToken.None));
    }
}
