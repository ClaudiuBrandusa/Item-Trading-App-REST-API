using Application.Results.Inventory;
using MediatR;

namespace Application.Behaviors.Inventory.GetLockedAmount;

public record GetInventoryItemLockedAmountQuery : IRequest<LockedItemAmountResult>
{
    public required string UserId { get; set; }

    public required string ItemId { get; set; }
}
