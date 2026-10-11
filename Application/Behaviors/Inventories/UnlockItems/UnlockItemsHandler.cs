using MediatR;
using Application.Services.Inventories;
using Application.Results.Inventories;

namespace Application.Behaviors.Inventories.UnlockItem;

public class UnlockItemsHandler : IRequestHandler<UnlockItemsCommand, Result<LockItemResult[]>>
{
    private readonly IInventoryService _inventoryService;

    public UnlockItemsHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<Result<LockItemResult[]>> Handle(UnlockItemsCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.UnlockItemsAsync(request, cancellationToken);
    }
}
