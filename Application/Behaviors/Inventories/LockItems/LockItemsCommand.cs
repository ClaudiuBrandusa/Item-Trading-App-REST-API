using Application.Results.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.LockItems;

public record LockItemsCommand : IRequest<Result<LockItemsResult>>
{
    public string UserId { get; set; } = string.Empty;

    public required (string itemId, int quantity)[] Items { get; set; }
}
