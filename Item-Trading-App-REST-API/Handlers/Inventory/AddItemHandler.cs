using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Models.Inventory;
using MapsterMapper;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public class AddItemHandler : HandlerBase, IRequestHandler<AddItemQuery, QuantifiedItemResult>
{
    public AddItemHandler(IServiceProvider serviceProvider, IMapper mapper) : base(serviceProvider, mapper)
    {
    }

    public Task<QuantifiedItemResult> Handle(AddItemQuery request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService, QuantifiedItemResult>(async (inventoryService) =>
            await inventoryService.AddItemAsync(Map<AddItemQuery, AddItem>(request), true)
        );
    }
}
