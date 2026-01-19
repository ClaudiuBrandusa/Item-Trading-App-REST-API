using Application.Results.Inventories;
using Application.Services.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.GetLockedAmount;

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
