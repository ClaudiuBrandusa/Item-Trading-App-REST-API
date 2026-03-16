using MediatR;
using Application.Services.Inventories;
using Application.Results.Inventories;

namespace Application.Behaviors.Inventories.LockItems;

public class LockItemsHandler : IRequestHandler<LockItemsCommand, LockItemsResult>
{
    private readonly IInventoryService _inventoryService;

    public LockItemsHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<LockItemsResult> Handle(LockItemsCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.LockItemsAsync(request);
    }
}
