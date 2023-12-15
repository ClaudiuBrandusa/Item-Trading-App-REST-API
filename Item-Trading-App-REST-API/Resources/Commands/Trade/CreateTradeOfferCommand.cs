using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Models.Trade;
using MediatR;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Resources.Commands.Trade;

public record CreateTradeOfferCommand : IRequest<SentTradeOfferResult>
{
    public string SenderUserId { get; set; }

    public string TargetUserId { get; set; }

    public IEnumerable<ItemPrice> Items { get; set; }
}
