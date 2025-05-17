using MediatR;

namespace Application.Behaviors.TradeItemHistory.GetTradeItems;

public record GetTradeItemsHistoryQuery : IRequest<Domain.Entities.Trades.TradeItem[]>
{
    public string TradeId { get; set; } = string.Empty;
}
