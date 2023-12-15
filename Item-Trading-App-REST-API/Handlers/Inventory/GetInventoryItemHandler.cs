using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Inventory;

public class GetInventoryItemHandler : HandlerBase, IRequestHandler<GetInventoryItemQuery, QuantifiedItemResult>
{
    public GetInventoryItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<QuantifiedItemResult> Handle(GetInventoryItemQuery request, CancellationToken cancellationToken)
    {
        return Execute(async (IInventoryService inventoryService) =>
            await inventoryService.GetItemAsync(request)
        );
    }
}
