using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Services.Inventory;
using MapsterMapper;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public class HasItemQuantityHandler : HandlerBase, IRequestHandler<HasItemQuantityQuery, bool>
{
    public HasItemQuantityHandler(IServiceProvider serviceProvider, IMapper mapper) : base(serviceProvider, mapper)
    {
    }

    public Task<bool> Handle(HasItemQuantityQuery request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService, bool>(async (inventoryService) =>
            await inventoryService.HasItemAsync(Map<HasItemQuantityQuery, HasItem>(request))
        );
    }
}
