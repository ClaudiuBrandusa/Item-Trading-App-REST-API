using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Commands.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Item;

public class UpdateItemHandler : IRequestHandler<UpdateItemCommand, FullItemResult>
{
    private readonly IItemService _itemService;

    public UpdateItemHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<FullItemResult> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        return _itemService.UpdateItemAsync(request);
    }
}
