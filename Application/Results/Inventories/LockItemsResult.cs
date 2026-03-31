using Application.Models.TradeItems;

namespace Application.Results.Inventories;

public record LockItemsResult(
    string UserId,
    TradeItemDTO[] Items
);
