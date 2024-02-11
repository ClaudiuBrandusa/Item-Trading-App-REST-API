using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Resources.Events.Trades;
using Item_Trading_App_REST_API.Resources.Queries.Identity;
using Item_Trading_App_REST_API.Services.Notification;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Events.Trade;

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
