using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using Item_Trading_App_REST_API.Models.Inventory;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public class DropItemHandler : HandlerBase, IRequestHandler<DropItemQuery, QuantifiedItemResult>
{
    public DropItemHandler(IServiceProvider serviceProvider, IMapper mapper) : base(serviceProvider, mapper)
    {
    }

    public Task<QuantifiedItemResult> Handle(DropItemQuery request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService, QuantifiedItemResult>(async (inventoryService) =>
            await inventoryService.DropItemAsync(Map<DropItemQuery, DropItem>(request), true)
        );
    }
}
