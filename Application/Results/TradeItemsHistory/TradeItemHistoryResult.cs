using Application.Models;

namespace Application.Results.TradeItemsHistory;

public record TradeItemHistoryResult : Result
{
    public string TradeId { get; set; } = string.Empty;
}
