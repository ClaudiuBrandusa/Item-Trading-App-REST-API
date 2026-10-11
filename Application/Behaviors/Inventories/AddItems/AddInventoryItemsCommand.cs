using Application.Results.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.AddItem;

public record AddInventoryItemsCommand : IRequest<Result<QuantifiedItemResult[]>>
{
    public string UserId { get; set; } = string.Empty;

    public required (string itemId, int quantity)[] Items { get; set; } = [];

    public bool Notify { get; set; }
}
