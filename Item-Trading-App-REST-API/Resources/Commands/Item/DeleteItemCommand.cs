using Item_Trading_App_REST_API.Models.Item;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Commands.Item;

public record DeleteItemCommand : IRequest<DeleteItemResult>
{
    public string ItemId { get; set; }

    public string UserId { get; set; }
}
