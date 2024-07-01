using Application.Models.Trade;
using MediatR;

namespace Application.Behaviors.Trade.ListTrades;

public record ListTradesQuery : IRequest<TradeOffersResult>
{
    public string UserId { get; set; }

    public string[] TradeItemIds { get; set; } = Array.Empty<string>();

    public TradeDirection TradeDirection { get; set; }

    public bool Responded { get; set; }
}
