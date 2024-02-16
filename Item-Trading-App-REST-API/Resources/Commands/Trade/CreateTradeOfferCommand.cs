using Item_Trading_App_REST_API.Models.Trade;
using MediatR;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Resources.Commands.Trade;

public record CreateTradeOfferCommand : IRequest<TradeOfferResult>
{
    public string SenderUserId { get; set; }

    public string TargetUserId { get; set; }

    public IEnumerable<Models.TradeItems.TradeItem> Items { get; set; }
}
