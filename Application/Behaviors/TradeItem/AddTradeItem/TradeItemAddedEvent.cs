using MediatR;

namespace Application.Behaviors.TradeItem.AddTradeItem;

public record TradeItemAddedEvent : INotification
{
    public string TradeId { get; set; }

    public Domain.TradeItems.TradeItem Data { get; set; }
}
