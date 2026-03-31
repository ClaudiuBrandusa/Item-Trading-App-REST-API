using Application.Results.Items;
using Application.Services.Item;
using MediatR;

namespace Application.Behaviors.Item.UpdateItem;

public class UpdateItemHandler : IRequestHandler<UpdateItemCommand, Result<FullItemResult>>
{
    private readonly IItemService _itemService;

    public UpdateItemHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<Result<FullItemResult>> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        return _itemService.UpdateItemAsync(request);
    }
}
