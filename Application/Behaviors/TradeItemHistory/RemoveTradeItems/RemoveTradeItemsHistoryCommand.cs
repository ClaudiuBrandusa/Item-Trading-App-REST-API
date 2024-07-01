using Application.Models.TradeItemHistory;
using MediatR;

namespace Application.Behaviors.TradeItemHistory.RemoveTradeItems;

public record RemoveTradeItemsHistoryCommand : IRequest<TradeItemHistoryBaseResult>
{
    public string TradeId { get; set; }
}
