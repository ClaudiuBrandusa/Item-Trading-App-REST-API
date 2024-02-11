using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

public class ListInventoryItemsHandler : HandlerBase, IRequestHandler<ListInventoryItemsQuery, ItemsResult>
{
    public ListInventoryItemsHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<ItemsResult> Handle(ListInventoryItemsQuery request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService, ItemsResult>(async (inventoryService) =>
            await inventoryService.ListItemsAsync(request)
        );
    }
}
