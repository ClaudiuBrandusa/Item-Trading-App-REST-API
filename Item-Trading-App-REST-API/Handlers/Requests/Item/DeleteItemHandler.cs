using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Commands.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Item;

public class DeleteItemHandler : HandlerBase, IRequestHandler<DeleteItemCommand, DeleteItemResult>
{
    public DeleteItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<DeleteItemResult> Handle(DeleteItemCommand request, CancellationToken cancellationToken)
    {
        return Execute<IItemService, DeleteItemResult>(async itemService =>
            await itemService.DeleteItemAsync(request)
        );
    }
}
