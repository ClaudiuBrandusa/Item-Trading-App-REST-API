using Application.Results.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.DropItem;

public record DropInventoryItemsCommand : IRequest<Result<QuantifiedItemResult[]>>
{
    public required string UserId { get; set; }

    public required (string itemId, int quantity)[] Items { get; set; } = [];

    public bool Notify { get; set; }
}
