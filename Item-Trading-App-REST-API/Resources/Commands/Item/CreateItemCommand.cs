using Item_Trading_App_REST_API.Models.Item;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Commands.Item;

public record CreateItemCommand : IRequest<FullItemResult>
{
    public string SenderUserId { get; set; }

    public string ItemName { get; set; }

    public string ItemDescription { get; set; }
}
