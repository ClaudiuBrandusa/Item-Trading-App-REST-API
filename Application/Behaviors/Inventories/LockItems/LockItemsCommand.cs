using Application.Results.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.LockItems;

public record LockItemsCommand : IRequest<LockItemsResult>
{
    public string UserId { get; set; } = string.Empty;

    public (string itemId, int quantity)[] Items { get; set; }
}
