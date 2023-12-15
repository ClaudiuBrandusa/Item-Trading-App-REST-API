using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Item;

public class GetItemNameHandler : HandlerBase, IRequestHandler<GetItemNameQuery, string>
{
    public GetItemNameHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<string> Handle(GetItemNameQuery request, CancellationToken cancellationToken)
    {
        return Execute<IItemService, string>(async (itemService) =>
            await itemService.GetItemNameAsync(request)
        );
    }
}
