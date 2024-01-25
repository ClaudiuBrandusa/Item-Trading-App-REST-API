using Item_Trading_App_REST_API.Models.Base;

namespace Item_Trading_App_REST_API.Models.TradeItemHistory;

public record TradeItemHistoryBaseResult : BaseResult
{
    public string TradeId { get; set; }
}
