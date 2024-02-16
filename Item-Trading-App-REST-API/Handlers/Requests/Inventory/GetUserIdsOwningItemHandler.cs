using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

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
