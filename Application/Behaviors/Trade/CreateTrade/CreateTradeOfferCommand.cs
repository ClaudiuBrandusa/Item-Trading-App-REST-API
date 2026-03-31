using Application.Models.TradeItems;
using Application.Results.Trades;
using MediatR;

namespace Application.Behaviors.Trade.CreateTrade;

public record CreateTradeOfferCommand : IRequest<Result<TradeOfferResult>>
{
    public required string SenderUserId { get; set; }

    public required string TargetUserId { get; set; }

    public required IEnumerable<TradeItemDTO> Items { get; set; }
}
