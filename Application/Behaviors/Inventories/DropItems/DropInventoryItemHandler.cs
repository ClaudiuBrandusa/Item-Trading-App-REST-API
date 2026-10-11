using MediatR;
using Application.Services.Inventories;
using Application.Results.Inventories;

namespace Application.Behaviors.Inventories.DropItem;

public class DropInventoryItemsHandler : IRequestHandler<DropInventoryItemsCommand, Result<QuantifiedItemResult[]>>
{
    private readonly IInventoryService _inventoryService;

    public DropInventoryItemsHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<Result<QuantifiedItemResult[]>> Handle(DropInventoryItemsCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.DropItemsAsync(request, cancellationToken);
    }
}
