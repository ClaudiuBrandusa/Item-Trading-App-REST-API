using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Commands.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Trade;

public class CancelTradeHandler : HandlerBase, IRequestHandler<CancelTradeCommand, CancelTradeOfferResult>
{
    public CancelTradeHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<CancelTradeOfferResult> Handle(CancelTradeCommand request, CancellationToken cancellationToken)
    {
        return Execute<ITradeService, CancelTradeOfferResult>(async tradeService =>
            await tradeService.CancelTradeOffer(request)
        );
    }
}
