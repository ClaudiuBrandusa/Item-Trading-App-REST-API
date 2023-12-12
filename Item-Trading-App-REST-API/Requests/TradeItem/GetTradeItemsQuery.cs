using MediatR;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Requests.TradeItem;

public record GetTradeItemsQuery : IRequest<List<Models.Trade.TradeItem>>
{
    public string TradeId { get; set; }
}
