using Application.Services.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.HasItem;

public class HasItemQuantityHandler : IRequestHandler<HasItemQuantityQuery, bool>
{
    private readonly IInventoryService _inventoryService;

    public HasItemQuantityHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<bool> Handle(HasItemQuantityQuery request, CancellationToken cancellationToken)
    {
        return _inventoryService.HasItemAsync(request);
    }
}
