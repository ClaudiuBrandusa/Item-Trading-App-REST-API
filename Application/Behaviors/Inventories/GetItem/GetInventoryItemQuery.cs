using Application.Results.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.GetItem;

public record GetInventoryItemQuery : IRequest<Result<QuantifiedItemResult>>
{
    public required string UserId { get; set; }

    public required string ItemId { get; set; }
}
