using MediatR;
using Application.Services.Inventories;
using Application.Results.Inventories;

namespace Application.Behaviors.Inventories.LockItem;

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
