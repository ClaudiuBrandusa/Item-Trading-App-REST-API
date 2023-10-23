using System;

namespace Item_Trading_App_REST_API.Models.Trade;

public record SentTradeOffer : BaseTradeOffer
{
    public string ReceiverId { get; set; }

    public string ReceiverName { get; set; }

    public DateTime SentDate { get; set; }
}
