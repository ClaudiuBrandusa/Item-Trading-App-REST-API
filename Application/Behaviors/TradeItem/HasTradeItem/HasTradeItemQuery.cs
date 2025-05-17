using MediatR;

namespace Application.Behaviors.TradeItem.HasTradeItem;

public record HasTradeItemQuery : IRequest<bool>
{
    public string TradeId { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;
}
