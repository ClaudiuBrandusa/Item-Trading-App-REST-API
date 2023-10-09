using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public class HasItemQuantityHandler : HandlerBase, IRequestHandler<HasItemQuantityQuery, bool>
{
    public HasItemQuantityHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<bool> Handle(HasItemQuantityQuery request, CancellationToken cancellationToken)
    {
        return await Execute<IInventoryService, bool>(async (inventoryService) =>
            await inventoryService.HasItemAsync(request.UserId, request.ItemId, request.Quantity)
        );
    }
}
