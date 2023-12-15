using System;

namespace Item_Trading_App_REST_API.Models.Trade;

public record RespondedSentTradeOfferResult : SentTradeOfferResult
{
    public bool Response { get; set; }

    public DateTime ResponseDate { get; set; }
}
