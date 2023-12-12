using MediatR;

namespace Item_Trading_App_REST_API.Requests.Trade;

public record ItemUsedInTradeQuery : IRequest<bool>
{
    public string ItemId { get; set; }
}
