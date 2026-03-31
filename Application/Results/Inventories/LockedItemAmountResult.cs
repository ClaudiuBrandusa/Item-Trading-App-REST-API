namespace Application.Results.Inventories;

public record LockedItemAmountResult
{
    public string ItemId { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public int Amount { get; set; }
}
