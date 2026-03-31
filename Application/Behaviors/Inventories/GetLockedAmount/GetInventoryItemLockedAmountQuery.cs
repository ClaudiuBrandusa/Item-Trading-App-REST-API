using Application.Results.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.GetLockedAmount;

public record GetInventoryItemLockedAmountQuery : IRequest<Result<LockedItemAmountResult>>
{
    public required string UserId { get; set; }

    public required string ItemId { get; set; }
}
