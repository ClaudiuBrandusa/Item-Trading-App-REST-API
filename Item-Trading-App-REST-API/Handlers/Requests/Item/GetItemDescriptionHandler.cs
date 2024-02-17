using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Item;

public class GetItemDescriptionHandler : IRequestHandler<GetItemDescriptionQuery, string>
{
    private readonly IItemService _itemService;

    public GetItemDescriptionHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<string> Handle(GetItemDescriptionQuery request, CancellationToken cancellationToken)
    {
        return _itemService.GetItemDescriptionAsync(request);
    }
}
