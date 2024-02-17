using Item_Trading_App_REST_API.Models.Trade;
using MediatR;
using System;

namespace Item_Trading_App_REST_API.Resources.Queries.Trade;

public record ListTradesQuery : IRequest<TradeOffersResult>
{
    public string UserId { get; set; }

    public string[] TradeItemIds { get; set; } = Array.Empty<string>();

    public TradeDirection TradeDirection { get; set; }

    public bool Responded { get; set; }
}
