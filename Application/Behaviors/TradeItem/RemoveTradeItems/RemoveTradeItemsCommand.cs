using MediatR;

namespace Application.Behaviors.TradeItem.RemoveTradeItems;

public record RemoveTradeItemsCommand : IRequest<bool>
{
    public string TradeId { get; set; } = string.Empty;

    public bool KeepCache { get; set; }
}
