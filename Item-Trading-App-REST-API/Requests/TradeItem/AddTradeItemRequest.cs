using MediatR;

namespace Item_Trading_App_REST_API.Requests.TradeItem;

public record AddTradeItemRequest : IRequest<bool>
{
    public string ItemId { get; set; }

    public string Name { get; set; }

    public int Quantity { get; set; }

    public int Price { get; set; }

    public string TradeId { get; set; }
}
