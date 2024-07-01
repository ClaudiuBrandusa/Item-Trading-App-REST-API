using Application.Models.Inventory;
using Application.Services.Inventory;
using MediatR;

namespace Application.Behaviors.Inventory.GetItem;

public class GetInventoryItemHandler : IRequestHandler<GetInventoryItemQuery, QuantifiedItemResult>
{
    private readonly IInventoryService _inventoryService;

    public GetInventoryItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<QuantifiedItemResult> Handle(GetInventoryItemQuery request, CancellationToken cancellationToken)
    {
        return _inventoryService.GetItemAsync(request);
    }
}
