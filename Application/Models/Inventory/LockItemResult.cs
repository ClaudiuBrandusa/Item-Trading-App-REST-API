using Application.Models.Base;

namespace Application.Models.Inventory;

public record LockItemResult : BaseResult
{
    public string UserId { get; set; }

    public string ItemId { get; set; }

    public int Quantity { get; set; }
}
