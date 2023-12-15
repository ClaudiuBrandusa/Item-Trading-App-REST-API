using MediatR;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Resources.Queries.TradeItem;

public record GetTradeItemsQuery : IRequest<List<Models.TradeItems.TradeItem>>
{
    public string TradeId { get; set; }
}
