using Application.Models;
using Application.Models.TradeItems;

namespace Application.Results.Inventories;

public record LockItemsResult : Result
{
    public string UserId { get; set; } = string.Empty;

    public TradeItemDTO[] Items { get; set; }
}
