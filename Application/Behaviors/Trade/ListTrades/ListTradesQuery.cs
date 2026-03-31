using Application.Models.Trades;
using Application.Results.Trades;
using MediatR;

namespace Application.Behaviors.Trade.ListTrades;

public record ListTradesQuery : IRequest<Result<TradeOffersResult>>
{
    public string UserId { get; set; } = string.Empty;

    public string[] TradeItemIds { get; set; } = Array.Empty<string>();

    public TradeDirection TradeDirection { get; set; }

    public bool Responded { get; set; }
}
