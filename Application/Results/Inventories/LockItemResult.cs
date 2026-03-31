namespace Application.Results.Inventories;

public record LockItemResult
{
    public string UserId { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
