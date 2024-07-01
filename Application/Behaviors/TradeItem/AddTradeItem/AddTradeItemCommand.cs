using MediatR;

namespace Application.Behaviors.TradeItem.AddTradeItem;

public record AddTradeItemCommand : IRequest<bool>
{
    public string ItemId { get; set; }

    public string Name { get; set; }

    public int Quantity { get; set; }

    public int Price { get; set; }

    public string TradeId { get; set; }

    public string UserId { get; set; }
}
