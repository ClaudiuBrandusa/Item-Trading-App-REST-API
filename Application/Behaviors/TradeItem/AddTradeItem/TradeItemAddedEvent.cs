using MediatR;

namespace Application.Behaviors.TradeItem.AddTradeItem;

public record TradeItemAddedEvent : INotification
{
    public string TradeId { get; set; } = string.Empty;

    public Domain.Entities.Trades.TradeItem Data { get; set; }
}
