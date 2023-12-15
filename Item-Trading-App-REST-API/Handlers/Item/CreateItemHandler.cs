using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Commands.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Item_Trading_App_REST_API.Handlers.Item;

public class CreateItemHandler : HandlerBase, IRequestHandler<CreateItemCommand, FullItemResult>
{
    public CreateItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<FullItemResult> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        return Execute<IItemService, FullItemResult>(async itemService =>
            await itemService.CreateItemAsync(request)
        );
    }
}
