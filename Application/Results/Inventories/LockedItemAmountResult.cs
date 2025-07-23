using Application.Models;

namespace Application.Results.Inventories;

public record LockedItemAmountResult : Result
{
    public string ItemId { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public int Amount { get; set; }
}
