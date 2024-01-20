using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.TradeItem;

public record HasTradeItemQuery : IRequest<bool>
{
    public string TradeId { get; set; }

    public string ItemId {  get; set; }
}
