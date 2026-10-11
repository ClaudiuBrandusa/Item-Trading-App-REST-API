using MediatR;
using Application.Services.Item;
using Application.Results.Items;

namespace Application.Behaviors.Item.GetItem;

public class GetItemsHandler : IRequestHandler<GetItemsQuery, Result<FullItemResult[]>>
{
    private readonly IItemService _itemService;

    public GetItemsHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<Result<FullItemResult[]>> Handle(GetItemsQuery request, CancellationToken cancellationToken)
    {
        return _itemService.GetItemsAsync(request, cancellationToken);
    }
}
