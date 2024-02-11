using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Commands.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Item;

public class UpdateItemHandler : HandlerBase, IRequestHandler<UpdateItemCommand, FullItemResult>
{
    public UpdateItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<FullItemResult> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        return Execute<IItemService, FullItemResult>(async itemService =>
            await itemService.UpdateItemAsync(request)
        );
    }
}
