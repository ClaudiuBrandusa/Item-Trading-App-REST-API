using Application.Results.TradeItemsHistory;
using MediatR;

namespace Application.Behaviors.TradeItemHistory.RemoveTradeItems;

public record RemoveTradeItemsHistoryCommand : IRequest<TradeItemHistoryResult>
{
    public string TradeId { get; set; } = string.Empty;
}
