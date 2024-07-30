using Application.Results.TradeItemsHistory;
using MediatR;

namespace Application.Behaviors.TradeItemHistory.AddTradeItems;

public record AddTradeItemsHistoryCommand : IRequest<TradeItemHistoryResult>
{
    public string TradeId { get; set; } = string.Empty;

    public Domain.Entities.Trades.TradeItem[] TradeItems { get; set; } = Array.Empty<Domain.Entities.Trades.TradeItem>();
}
