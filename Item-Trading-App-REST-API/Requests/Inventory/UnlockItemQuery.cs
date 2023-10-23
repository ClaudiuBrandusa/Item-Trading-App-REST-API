using Item_Trading_App_REST_API.Models.Inventory;
using MediatR;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public record UnlockItemQuery : IRequest<LockItemResult>
{
    public string UserId { get; set; }

    public string ItemId { get; set; }

    public int Quantity { get; set; }
}
