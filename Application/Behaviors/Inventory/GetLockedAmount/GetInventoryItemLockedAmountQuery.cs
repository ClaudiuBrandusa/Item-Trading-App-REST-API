using Application.Models.Inventory;
using MediatR;

namespace Application.Behaviors.Inventory.GetLockedAmount;

public record GetInventoryItemLockedAmountQuery : IRequest<LockedItemAmountResult>
{
    public string UserId { get; set; }

    public string ItemId { get; set; }
}
