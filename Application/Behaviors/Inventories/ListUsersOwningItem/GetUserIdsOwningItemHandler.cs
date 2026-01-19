using Application.Models.Inventories;
using Application.Services.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.ListUsersOwningItem;

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
