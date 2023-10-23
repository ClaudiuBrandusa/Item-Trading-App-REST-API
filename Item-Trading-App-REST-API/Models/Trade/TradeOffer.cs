using System;

namespace Item_Trading_App_REST_API.Models.Trade;

public record TradeOffer : BaseTradeOffer
{
    public string SenderId { get; set; }

    public string ReceiverId { get; set; }

    public DateTime SentDate { get; set; }

    public DateTime? ResponseDate { get; set; }

    public bool? Response { get; set; }
}
