using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.Item;

public record GetTradesUsingTheItemQuery : IRequest<string[]>
{
    public string ItemId { get; set; }
}
