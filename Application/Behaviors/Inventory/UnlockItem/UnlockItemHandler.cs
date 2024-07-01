using MediatR;
using Application.Models.Inventory;
using Application.Services.Inventory;

namespace Application.Behaviors.Inventory.UnlockItem;

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
