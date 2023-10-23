using Item_Trading_App_REST_API.Models.Base;

namespace Item_Trading_App_REST_API.Models.Trade;

public record TradeOfferResult : BaseResult
{
    public string TradeOfferId { get; set; }
}
