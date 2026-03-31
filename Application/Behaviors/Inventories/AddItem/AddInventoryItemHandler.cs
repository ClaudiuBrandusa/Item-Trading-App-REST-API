using Application.Results.Inventories;
using Application.Services.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.AddItem;

public class AddInventoryItemHandler : IRequestHandler<AddInventoryItemCommand, Result<QuantifiedItemResult>>
{
    private readonly IInventoryService _inventoryService;

    public AddInventoryItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<Result<QuantifiedItemResult>> Handle(AddInventoryItemCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.AddItemAsync(request);
    }
}
