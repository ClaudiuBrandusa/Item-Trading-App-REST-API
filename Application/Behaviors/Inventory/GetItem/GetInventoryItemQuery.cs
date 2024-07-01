using Application.Models.Inventory;
using MediatR;

namespace Application.Behaviors.Inventory.GetItem;

public record GetInventoryItemQuery : IRequest<QuantifiedItemResult>
{
    public required string UserId { get; set; }

    public required string ItemId { get; set; }
}
