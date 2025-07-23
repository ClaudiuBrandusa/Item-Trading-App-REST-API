using Application.Models;

namespace Application.Results.Inventories;

public record LockItemResult : Result
{
    public string UserId { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
