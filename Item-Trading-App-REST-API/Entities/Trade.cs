using System;

namespace Item_Trading_App_REST_API.Entities
{
    public class Trade
    {
        public string TradeId { get; set; }

        public DateTime SentDate { get; set; }

        public DateTime? ResponseDate { get; set; }

        public bool? Response { get; set; }
    }
}
