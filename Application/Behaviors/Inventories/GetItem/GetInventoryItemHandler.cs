using Application.Results.Inventories;
using Application.Services.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.GetItem;

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
