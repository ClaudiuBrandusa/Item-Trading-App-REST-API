using Application.Results.Trades;
using MediatR;

namespace Application.Behaviors.Trade.GetTrade;

public record RequestTradeOfferQuery : IRequest<Result<TradeOfferResult>>
{
    public string TradeId { get; set; } = string.Empty;
}
