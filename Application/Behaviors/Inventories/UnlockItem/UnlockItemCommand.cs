using Application.Results.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.UnlockItem;

public record UnlockItemCommand : IRequest<Result<LockItemResult>>
{
    public string UserId { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public bool Notify { get; set; }
}
