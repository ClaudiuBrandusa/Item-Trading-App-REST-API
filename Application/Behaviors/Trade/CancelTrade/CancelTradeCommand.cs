using Application.Models.Trade;
using MediatR;

namespace Application.Behaviors.Trade.CancelTrade;

public record CancelTradeCommand : IRequest<TradeOfferResult>
{
    public string UserId { get; set; }

    public string TradeId { get; set; }
}
