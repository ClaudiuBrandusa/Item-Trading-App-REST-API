using Item_Trading_App_REST_API.Models.Item;
using MediatR;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Requests.TradeItem;

public record GetItemPricesQuery : IRequest<List<ItemPrice>>
{
    public string TradeId { get; set; }
}
