using MediatR;

namespace Application.Behaviors.TradeItemHistory.GetTradeItems;

public record GetTradeItemsHistoryQuery : IRequest<Domain.TradeItems.TradeItem[]>
{
    public string TradeId { get; set; }
}
