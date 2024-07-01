using MediatR;

namespace Application.Behaviors.TradeItem.GetTradeItems;

public record GetTradeItemsQuery : IRequest<Domain.TradeItems.TradeItem[]>
{
    public string TradeId { get; set; }
}
