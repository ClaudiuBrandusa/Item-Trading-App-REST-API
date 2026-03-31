using Application.Results.Items;
using Application.Services.Item;
using MediatR;

namespace Application.Behaviors.Item.CreateItem;

public class CreateItemHandler : IRequestHandler<CreateItemCommand, Result<FullItemResult>>
{
    private readonly IItemService _itemService;

    public CreateItemHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<Result<FullItemResult>> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        return _itemService.CreateItemAsync(request);
    }
}
