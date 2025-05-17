using Application.Results.Trades;
using MediatR;

namespace Application.Behaviors.Trade.RespondTrade;

public record RespondTradeCommand : IRequest<TradeOfferResult>
{
    public required string UserId { get; set; }

    public required string TradeId { get; set; }

    public bool Response { get; set; }
}
