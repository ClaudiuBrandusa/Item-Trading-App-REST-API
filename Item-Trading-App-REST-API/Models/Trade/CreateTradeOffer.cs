using Item_Trading_App_REST_API.Models.Item;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Models.Trade
{
    public class CreateTradeOffer
    {
        public string SenderUserId { get; set; }

        public string TargetUserId { get; set; }

        public IEnumerable<ItemPrice> Items { get; set; }
    }
}
