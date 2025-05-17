using MediatR;

namespace Application.Behaviors.TradeItem.GetTradeItems;

public record GetTradeItemsQuery : IRequest<Domain.Entities.Trades.TradeItem[]>
{
    public string TradeId { get; set; } = string.Empty;
}
