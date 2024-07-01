using MediatR;

namespace Application.Behaviors.TradeItem.RemoveTradeItems;

public record TradeItemRemovedEvent : INotification
{
    public string TradeId { get; set; }

    public bool KeepCache { get; set; }
}
