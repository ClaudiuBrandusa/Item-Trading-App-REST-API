using MediatR;

namespace Item_Trading_App_REST_API.Requests.Item;

public record GetItemDescriptionQuery : IRequest<string>
{
    public string ItemId { get; set; }
}
