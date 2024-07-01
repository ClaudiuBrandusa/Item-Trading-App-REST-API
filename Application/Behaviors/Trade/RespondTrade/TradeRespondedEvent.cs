using MediatR;

namespace Application.Behaviors.Trade.RespondTrade;

public record TradeRespondedEvent : INotification
{
    public required string TradeId { get; set; }

    public required string SenderId { get; set; }

    public bool Response { get; set; }
}
