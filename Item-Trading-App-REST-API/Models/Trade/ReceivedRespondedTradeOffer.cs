using System;

namespace Item_Trading_App_REST_API.Models.Trade;

public record ReceivedRespondedTradeOffer : ReceivedTradeOffer
{
    public bool Response { get; set; }

    public DateTime ResponseDate { get; set; }
}
