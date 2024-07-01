using Application.Models.Inventory;
using Application.Services.Inventory;
using MediatR;

namespace Application.Behaviors.Inventory.GetLockedAmount;

public class GetInventoryItemLockedAmountHandler : IRequestHandler<GetInventoryItemLockedAmountQuery, LockedItemAmountResult>
{
    private readonly IInventoryService _inventoryService;

    public GetInventoryItemLockedAmountHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<LockedItemAmountResult> Handle(GetInventoryItemLockedAmountQuery request, CancellationToken cancellationToken)
    {
        return _inventoryService.GetLockedAmountAsync(request);
    }
}
