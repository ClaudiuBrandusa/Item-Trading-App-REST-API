using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public class GetUserIdsOwningItemHandler : HandlerBase, IRequestHandler<GetUserIdsOwningItem, List<string>>
{
    public GetUserIdsOwningItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<List<string>> Handle(GetUserIdsOwningItem request, CancellationToken cancellationToken)
    {
        return await Execute<IInventoryService, List<string>>(async (inventoryService) =>
            (await inventoryService.GetUsersThatOwnThisItem(request.ItemId)).UserIds
        );
    }
}
