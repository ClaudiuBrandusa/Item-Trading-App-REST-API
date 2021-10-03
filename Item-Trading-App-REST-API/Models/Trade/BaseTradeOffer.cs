using Item_Trading_App_REST_API.Models.Base;
using Item_Trading_App_REST_API.Models.Item;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Models.Trade
{
    public class BaseTradeOffer : BaseResult
    {
        public string TradeOfferId { get; set; }

        public IEnumerable<ItemPrice> Items { get; set; }
    }
}
