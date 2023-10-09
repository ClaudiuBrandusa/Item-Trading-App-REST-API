﻿namespace Item_Trading_App_REST_API.Models.Trade;

public record CancelTradeOfferResult : TradeOfferResult
{
    public string ReceiverId { get; set; }

    public string ReceiverName { get; set; }
}
