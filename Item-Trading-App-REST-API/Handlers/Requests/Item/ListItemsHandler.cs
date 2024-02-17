using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Item;

public class ListItemsHandler : IRequestHandler<ListItemsQuery, ItemsResult>
{
    private readonly IItemService _itemService;

    public ListItemsHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<ItemsResult> Handle(ListItemsQuery request, CancellationToken cancellationToken)
    {
        return _itemService.ListItemsAsync(request);
    }
}
