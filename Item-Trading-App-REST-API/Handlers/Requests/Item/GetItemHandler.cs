using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Resources.Queries.Item;

namespace Item_Trading_App_REST_API.Handlers.Requests.Item;

public class GetItemHandler : IRequestHandler<GetItemQuery, FullItemResult>
{
    private readonly IItemService _itemService;

    public GetItemHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<FullItemResult> Handle(GetItemQuery request, CancellationToken cancellationToken)
    {
        return _itemService.GetItemAsync(request);
    }
}
