using Item_Trading_App_REST_API.Models.Base;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Models.Trade;

public record TradeOffersResult : BaseResult
{
    public IEnumerable<string> TradeOffers { get; set; }
}
