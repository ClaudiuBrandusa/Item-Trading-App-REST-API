using Application.Models.Inventory;
using Application.Services.Inventory;
using MediatR;

namespace Application.Behaviors.Inventory.ListUsersOwningItem;

public class GetUserIdsOwningItemHandler : IRequestHandler<GetUserIdsOwningItemQuery, UsersOwningItem>
{
    private readonly IInventoryService _inventoryService;

    public GetUserIdsOwningItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<UsersOwningItem> Handle(GetUserIdsOwningItemQuery request, CancellationToken cancellationToken)
    {
        return _inventoryService.GetUsersOwningThisItemAsync(request);
    }
}
