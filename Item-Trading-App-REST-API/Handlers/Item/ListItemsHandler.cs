using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Item;

public class ListItemsHandler : HandlerBase, IRequestHandler<ListItemsQuery, ItemsResult>
{
    public ListItemsHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<ItemsResult> Handle(ListItemsQuery request, CancellationToken cancellationToken)
    {
        return Execute<IItemService, ItemsResult>(async (itemService) =>
            await itemService.ListItemsAsync(request)
        );
    }
}
