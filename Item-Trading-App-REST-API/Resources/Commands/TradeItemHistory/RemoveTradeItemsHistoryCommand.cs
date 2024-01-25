using Item_Trading_App_REST_API.Models.TradeItemHistory;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Commands.TradeItemHistory;

public record RemoveTradeItemsHistoryCommand : IRequest<TradeItemHistoryBaseResult>
{
    public string TradeId { get; set; }
}
