using System;

namespace Item_Trading_App_REST_API.Models.Trade;

public record ReceivedTradeOffer : BaseTradeOffer
{
    public string SenderId { get; set; }

    public string SenderName { get; set; }

    public DateTime SentDate { get; set; }
}
