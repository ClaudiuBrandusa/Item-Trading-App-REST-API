using Item_Trading_App_REST_API.Models.Trade;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.Trade;

public record ListRespondedReceivedTradesQuery : ListTradesQuery, IRequest<TradeOffersResult>
{
}
