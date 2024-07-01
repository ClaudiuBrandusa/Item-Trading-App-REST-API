using MediatR;

namespace Application.Behaviors.TradeItem.GetTradeItemIds;

public record GetTradesUsingTheItemQuery : IRequest<string[]>
{
    public string ItemId { get; set; }
}
