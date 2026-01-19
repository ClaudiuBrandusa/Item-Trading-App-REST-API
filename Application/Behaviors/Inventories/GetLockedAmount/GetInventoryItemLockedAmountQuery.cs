using Application.Results.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.GetLockedAmount;

public record GetInventoryItemLockedAmountQuery : IRequest<LockedItemAmountResult>
{
    public required string UserId { get; set; }

    public required string ItemId { get; set; }
}
