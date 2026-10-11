using Application.Services.Item;
using MediatR;

namespace Application.Behaviors.Item.GetItemName;

public class GetItemsNameHandler : IRequestHandler<GetItemsNameQuery, Dictionary<string, string>>
{
    private readonly IItemService _itemService;

    public GetItemsNameHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<Dictionary<string, string>> Handle(GetItemsNameQuery request, CancellationToken cancellationToken)
    {
        return _itemService.GetItemsNameAsync(request, cancellationToken);
    }
}
