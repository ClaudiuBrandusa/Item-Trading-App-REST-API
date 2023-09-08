using System;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Models.Trade;

public record CachedTrade
{
    public string TradeId { get; set; }

    public string SenderUserId { get; set; }

    public string ReceiverUserId { get; set; }

    public bool? Response { get; set; } = null;

    public DateTime SentDate { get; set; }

    public DateTime? ResponseDate { get; set; } = null;

    public List<string> TradeItemsId { get; set; } = new List<string>();
}
