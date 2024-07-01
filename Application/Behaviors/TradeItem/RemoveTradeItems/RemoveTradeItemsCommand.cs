using MediatR;

namespace Application.Behaviors.TradeItem.RemoveTradeItems;

public record RemoveTradeItemsCommand : IRequest<bool>
{
    public string TradeId { get; set; }

    public bool KeepCache { get; set; }
}
