using Item_Trading_App_REST_API.Models.Item;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.Item;

public record ListItemsQuery : IRequest<ItemsResult>
{
    public string SearchString { get; set; }
}
