using Application.Results.Inventory;
using Application.Services.Inventory;
using MediatR;

namespace Application.Behaviors.Inventory.AddItem;

public class AddInventoryItemHandler : IRequestHandler<AddInventoryItemCommand, QuantifiedItemResult>
{
    private readonly IInventoryService _inventoryService;

    public AddInventoryItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<QuantifiedItemResult> Handle(AddInventoryItemCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.AddItemAsync(request);
    }
}
