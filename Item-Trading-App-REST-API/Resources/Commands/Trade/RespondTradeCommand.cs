﻿using Item_Trading_App_REST_API.Models.Trade;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Commands.Trade;

public record RespondTradeCommand : IRequest<TradeOfferResult>
{
    public string UserId { get; set; }

    public string TradeId { get; set; }

    public bool Response { get; set; }
}
