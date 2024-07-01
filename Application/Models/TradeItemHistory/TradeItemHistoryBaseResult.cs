using Application.Models.Base;

namespace Application.Models.TradeItemHistory;

public record TradeItemHistoryBaseResult : BaseResult
{
    public string TradeId { get; set; }
}
