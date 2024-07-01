using Application.Models.Base;

namespace Application.Models.Inventory;

public record LockedItemAmountResult : BaseResult
{
    public string ItemId { get; set; }

    public string ItemName { get; set; }

    public int Amount { get; set; }
}
