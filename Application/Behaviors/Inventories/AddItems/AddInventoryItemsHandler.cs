using Application.Results.Inventories;
using Application.Services.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.AddItem;

public class AddInventoryItemsHandler : IRequestHandler<AddInventoryItemsCommand, Result<QuantifiedItemResult[]>>
{
    private readonly IInventoryService _inventoryService;

    public AddInventoryItemsHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<Result<QuantifiedItemResult[]>> Handle(AddInventoryItemsCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.AddItemsAsync(request, cancellationToken);
    }
}
