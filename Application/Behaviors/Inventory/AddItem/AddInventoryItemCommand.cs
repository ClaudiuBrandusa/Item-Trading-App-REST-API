using Application.Models.Inventory;
using MediatR;

namespace Application.Behaviors.Inventory.AddItem;

public record AddInventoryItemCommand : IRequest<QuantifiedItemResult>
{
    public string UserId { get; set; }

    public string ItemId { get; set; }

    public int Quantity { get; set; }

    public bool Notify { get; set; }
}
