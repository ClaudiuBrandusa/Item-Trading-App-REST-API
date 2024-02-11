using MediatR;

namespace Item_Trading_App_REST_API.Resources.Commands.TradeItem;

public record RemoveTradeItemsCommand : IRequest<bool>
{
    public string TradeId { get; set; }

    public bool KeepCache { get; set; }
}
