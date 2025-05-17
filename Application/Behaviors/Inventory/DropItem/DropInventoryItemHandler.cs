using MediatR;
using Application.Services.Inventory;
using Application.Results.Inventory;

namespace Application.Behaviors.Inventory.DropItem;

public class DropInventoryItemHandler : IRequestHandler<DropInventoryItemCommand, QuantifiedItemResult>
{
    private readonly IInventoryService _inventoryService;

    public DropInventoryItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<QuantifiedItemResult> Handle(DropInventoryItemCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.DropItemAsync(request);
    }
}
