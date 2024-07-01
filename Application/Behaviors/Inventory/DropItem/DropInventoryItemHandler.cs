using MediatR;
using Application.Models.Inventory;
using Application.Services.Inventory;

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
