using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Item;

public class GetItemHandler : HandlerBase, IRequestHandler<GetItemQuery, FullItemResult>
{
    public GetItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<FullItemResult> Handle(GetItemQuery request, CancellationToken cancellationToken)
    {
        return await Execute<IItemService, FullItemResult>(async (itemService) =>
            await itemService.GetItemAsync(request.ItemId)
        );
    }
}
