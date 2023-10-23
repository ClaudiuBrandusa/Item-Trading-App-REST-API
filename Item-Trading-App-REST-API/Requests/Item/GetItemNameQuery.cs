using MediatR;

namespace Item_Trading_App_REST_API.Requests.Item;

public record GetItemNameQuery : IRequest<string>
{
    public string ItemId { get; set; }
}
