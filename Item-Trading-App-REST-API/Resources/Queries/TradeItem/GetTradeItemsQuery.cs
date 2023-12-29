using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.TradeItem;

public record GetTradeItemsQuery : IRequest<Models.TradeItems.TradeItem[]>
{
    public string TradeId { get; set; }
}
