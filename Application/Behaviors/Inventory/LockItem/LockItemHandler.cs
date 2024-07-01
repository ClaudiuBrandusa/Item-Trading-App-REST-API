using MediatR;
using Application.Models.Inventory;
using Application.Services.Inventory;

namespace Application.Behaviors.Inventory.LockItem;

public class LockItemHandler : IRequestHandler<LockItemCommand, LockItemResult>
{
    private readonly IInventoryService _inventoryService;

    public LockItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<LockItemResult> Handle(LockItemCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.LockItemAsync(request);
    }
}
