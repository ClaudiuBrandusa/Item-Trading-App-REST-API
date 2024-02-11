using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Item;

public class GetItemDescriptionHandler : HandlerBase, IRequestHandler<GetItemDescriptionQuery, string>
{
    public GetItemDescriptionHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<string> Handle(GetItemDescriptionQuery request, CancellationToken cancellationToken)
    {
        return Execute<IItemService, string>(async (itemService) =>
            await itemService.GetItemDescriptionAsync(request)
        );
    }
}
