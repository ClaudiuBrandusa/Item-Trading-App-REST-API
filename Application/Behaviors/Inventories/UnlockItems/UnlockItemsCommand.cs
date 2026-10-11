using Application.Results.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.UnlockItem;

public record UnlockItemsCommand : IRequest<Result<LockItemResult[]>>
{
    public string UserId { get; set; } = string.Empty;

    public (string itemId, int quantity)[] Items = [];

    public bool Notify { get; set; }
}
