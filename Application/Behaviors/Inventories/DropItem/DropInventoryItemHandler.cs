using MediatR;
using Application.Services.Inventories;
using Application.Results.Inventories;

namespace Application.Behaviors.Inventories.DropItem;

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
