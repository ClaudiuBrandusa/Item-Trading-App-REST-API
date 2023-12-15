using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Trade;

public class RequestSentTradeOfferHandler : HandlerBase, IRequestHandler<RequestSentTradeOfferQuery, SentTradeOfferResult>
{
    public RequestSentTradeOfferHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<SentTradeOfferResult> Handle(RequestSentTradeOfferQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeService, SentTradeOfferResult>(async tradeService =>
            await tradeService.GetSentTradeOffer(request)
        );
    }
}
