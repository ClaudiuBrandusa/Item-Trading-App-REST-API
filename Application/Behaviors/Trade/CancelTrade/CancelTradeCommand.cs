using Application.Results.Trades;
using MediatR;

namespace Application.Behaviors.Trade.CancelTrade;

public record CancelTradeCommand : IRequest<TradeOfferResult>
{
    public required string UserId { get; set; }

    public required string TradeId { get; set; }
}
