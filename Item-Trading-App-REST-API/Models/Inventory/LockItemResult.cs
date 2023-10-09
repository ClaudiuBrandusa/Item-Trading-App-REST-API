using Item_Trading_App_REST_API.Models.Base;

namespace Item_Trading_App_REST_API.Models.Inventory;

public record LockItemResult : BaseResult
{
    public string UserId { get; set; }

    public string ItemId { get; set; }

    public int Quantity { get; set; }
}
