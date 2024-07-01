using Application.Services.Inventory;
using MediatR;

namespace Application.Behaviors.Inventory.RemoveItemFromUsers;

public class RemoveItemFromUsersHandler : IRequestHandler<RemoveItemFromUsersCommand>
{
    private readonly IInventoryService _inventoryService;

    public RemoveItemFromUsersHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task Handle(RemoveItemFromUsersCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.RemoveItemCacheAsync(request);
    }
}
