using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Trade;

public class RequestRespondedReceivedTradeOfferHandler : HandlerBase, IRequestHandler<RequestRespondedReceivedTradeOfferQuery, RespondedReceivedTradeOfferResult>
{
    public RequestRespondedReceivedTradeOfferHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<RespondedReceivedTradeOfferResult> Handle(RequestRespondedReceivedTradeOfferQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeService, RespondedReceivedTradeOfferResult>(async tradeService =>
            await tradeService.GetReceivedRespondedTradeOffer(request)
        );
    }
}
