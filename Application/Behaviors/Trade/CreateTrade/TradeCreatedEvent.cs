using MediatR;

namespace Application.Behaviors.Trade.CreateTrade;

public record TradeCreatedEvent : INotification
{
    public required string TradeId { get; set; }

    public required string ReceiverId { get; set; }
}
