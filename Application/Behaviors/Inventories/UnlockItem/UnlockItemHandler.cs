using MediatR;
using Application.Services.Inventories;
using Application.Results.Inventories;

namespace Application.Behaviors.Inventories.UnlockItem;

public class UnlockItemHandler : IRequestHandler<UnlockItemCommand, LockItemResult>
{
    private readonly IInventoryService _inventoryService;

    public UnlockItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<LockItemResult> Handle(UnlockItemCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.UnlockItemAsync(request);
    }
}
