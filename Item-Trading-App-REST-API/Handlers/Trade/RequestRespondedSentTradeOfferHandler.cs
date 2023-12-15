using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Trade;

public class RequestRespondedSentTradeOfferHandler : HandlerBase, IRequestHandler<RequestRespondedSentTradeOfferQuery, RespondedSentTradeOfferResult>
{
    public RequestRespondedSentTradeOfferHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<RespondedSentTradeOfferResult> Handle(RequestRespondedSentTradeOfferQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeService, RespondedSentTradeOfferResult>(async tradeService =>
            await tradeService.GetRespondedSentTradeOffer(request)
        );
    }
}
