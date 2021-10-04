using System;

namespace Item_Trading_App_REST_API.Models.Trade
{
    public class AcceptTradeOfferResult : TradeOfferResult
    {
        public string SenderId { get; set; }

        public string SenderName { get; set; }

        public DateTime ReceivedDate { get; set; } = DateTime.Now;

        public DateTime ResponseDate { get; set; } = DateTime.Now;
    }
}
