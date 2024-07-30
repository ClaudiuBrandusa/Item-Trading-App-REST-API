using Application.Results.Trades;
using MediatR;

namespace Application.Behaviors.Trade.GetTrade;

public record RequestTradeOfferQuery : IRequest<TradeOfferResult>
{
    public string TradeId { get; set; } = string.Empty;
}
