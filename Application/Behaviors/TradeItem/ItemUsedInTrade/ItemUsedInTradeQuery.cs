using MediatR;

namespace Application.Behaviors.TradeItem.ItemUsedInTrade;

public record ItemUsedInTradeQuery : IRequest<bool>
{
    public string ItemId { get; set; }
}
