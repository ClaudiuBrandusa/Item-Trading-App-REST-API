using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

public class HasItemQuantityHandler : HandlerBase, IRequestHandler<HasItemQuantityQuery, bool>
{
    public HasItemQuantityHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<bool> Handle(HasItemQuantityQuery request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService, bool>(async (inventoryService) =>
            await inventoryService.HasItemAsync(request)
        );
    }
}
