using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.TradeItemHistory;

public record GetTradeItemsHistoryQuery : IRequest<Models.TradeItems.TradeItem[]>
{
    public string TradeId { get; set; }
}
