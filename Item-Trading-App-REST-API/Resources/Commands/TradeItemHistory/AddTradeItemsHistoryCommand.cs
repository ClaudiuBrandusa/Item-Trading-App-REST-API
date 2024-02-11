using Item_Trading_App_REST_API.Models.TradeItemHistory;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Commands.TradeItemHistory;

public record AddTradeItemsHistoryCommand : IRequest<TradeItemHistoryBaseResult>
{
    public string TradeId {  get; set; }

    public Models.TradeItems.TradeItem[] TradeItems { get; set; }
}
