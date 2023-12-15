using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public class GetUserIdsOwningItemHandler : HandlerBase, IRequestHandler<GetUserIdsOwningItemQuery, UsersOwningItem>
{
    public GetUserIdsOwningItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<UsersOwningItem> Handle(GetUserIdsOwningItemQuery request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService, UsersOwningItem>(async (inventoryService) =>
            await inventoryService.GetUsersOwningThisItem(request)
        );
    }
}
