using System;

namespace Item_Trading_App_REST_API.Models.Trade;

public record SentTradeOfferResult : BaseTradeOfferResult
{
    public string ReceiverId { get; set; }

    public string ReceiverName { get; set; }

    public DateTime SentDate { get; set; }
}
