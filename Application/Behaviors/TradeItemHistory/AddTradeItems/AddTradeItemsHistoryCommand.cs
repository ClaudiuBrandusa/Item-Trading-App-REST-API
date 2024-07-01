using Application.Models.TradeItemHistory;
using MediatR;

namespace Application.Behaviors.TradeItemHistory.AddTradeItems;

public record AddTradeItemsHistoryCommand : IRequest<TradeItemHistoryBaseResult>
{
    public string TradeId { get; set; }

    public Domain.TradeItems.TradeItem[] TradeItems { get; set; }
}
