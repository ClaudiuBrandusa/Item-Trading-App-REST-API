using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Item;

public class GetItemNameHandler : IRequestHandler<GetItemNameQuery, string>
{
    private readonly IItemService _itemService;

    public GetItemNameHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<string> Handle(GetItemNameQuery request, CancellationToken cancellationToken)
    {
        return _itemService.GetItemNameAsync(request);
    }
}
