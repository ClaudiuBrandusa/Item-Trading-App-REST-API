using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Trade;

public class RequestReceivedTradeOfferHandler : HandlerBase, IRequestHandler<RequestReceivedTradeOfferQuery, ReceivedTradeOfferResult>
{
    public RequestReceivedTradeOfferHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<ReceivedTradeOfferResult> Handle(RequestReceivedTradeOfferQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeService, ReceivedTradeOfferResult>(async tradeService =>
            await tradeService.GetReceivedTradeOffer(request)
        );
    }
}
