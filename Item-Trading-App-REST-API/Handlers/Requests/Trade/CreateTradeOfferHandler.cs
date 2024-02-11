using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Resources.Commands.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Trade;

public class CreateTradeOfferHandler : HandlerBase, IRequestHandler<CreateTradeOfferCommand, SentTradeOfferResult>
{
    public CreateTradeOfferHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<SentTradeOfferResult> Handle(CreateTradeOfferCommand request, CancellationToken cancellationToken)
    {
        return Execute<ITradeService, SentTradeOfferResult>(async tradeService =>
            await tradeService.CreateTradeOfferAsync(request)
        );
    }
}
