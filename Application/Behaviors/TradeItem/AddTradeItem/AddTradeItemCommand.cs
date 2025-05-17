using MediatR;

namespace Application.Behaviors.TradeItem.AddTradeItem;

public record AddTradeItemCommand : IRequest<bool>
{
    public string ItemId { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int Price { get; set; }

    public string TradeId { get; set; } = string.Empty;
}
