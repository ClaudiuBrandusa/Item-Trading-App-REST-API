using Application.Models.Trade;
using MediatR;

namespace Application.Behaviors.Trade.CreateTrade;

public record CreateTradeOfferCommand : IRequest<TradeOfferResult>
{
    public required string SenderUserId { get; set; }

    public required string TargetUserId { get; set; }

    public required IEnumerable<Domain.TradeItems.TradeItem> Items { get; set; }
}
