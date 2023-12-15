using Item_Trading_App_REST_API.Models.Inventory;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Commands.Inventory;

public record UnlockItemCommand : IRequest<LockItemResult>
{
    public string UserId { get; set; }

    public string ItemId { get; set; }

    public int Quantity { get; set; }

    public bool Notify { get; set; }
}
