using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Trade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Item_Trading_App_Contracts.Responses.Trade;
using Item_Trading_App_Contracts.Responses.Base;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MediatR;
using System;
using Application.Behaviors.Trade.CreateTrade;
using Application.Behaviors.Trade.RespondTrade;
using Application.Behaviors.Trade.CancelTrade;
using Application.Behaviors.Trade.ListTrades;
using Application.Behaviors.Trade.GetTrade;
using Application.Results.Trades;
using Application.Models.Trades;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Item_Trading_App_REST_API.Controllers;

[Authorize]
public class TradeController : BaseController
{
    private readonly IMediator _mediator;

    public TradeController(ILogger<TradeController> logger, IMapper mapper, IMediator mediator) : base(logger, mapper)
    {
        _mediator = mediator;
    }

    [HttpGet(Endpoints.Trade.Get)]
    public async Task<IActionResult> Get(string tradeId, CancellationToken cancellationToken)
    {
        var model = AdaptToType<string, RequestTradeOfferQuery>(tradeId);

        var result = await _mediator.Send(model, cancellationToken);

        return MapResult<TradeOfferResult, TradeOfferSuccessResponse, TradeOfferFailedResponse>(result);
    }

    [HttpGet(Endpoints.Trade.List)]
    public async Task<IActionResult> List([FromQuery] string[] tradeItemIds, [FromQuery] string direction, CancellationToken cancellationToken, [FromQuery] bool responded = false)
    {
        if (!Enum.TryParse<TradeDirection>(direction, out var tradeDirection))
            return new BadRequestObjectResult(new FailedResponse { Errors = ["Invalid trade direction value"] });

        var model = AdaptToType<string, ListTradesQuery>(UserId, (nameof(ListTradesQuery.TradeItemIds), tradeItemIds), (nameof(ListTradesQuery.TradeDirection), tradeDirection), (nameof(ListTradesQuery.Responded), responded));

        var results = await _mediator.Send(model, cancellationToken);

        return MapResult<TradeOffersResult, ListTradeOffersSuccessResponse, FailedResponse>(results);
    }

    [HttpGet(Endpoints.Trade.Directions)]
    public Task<IActionResult> GetTradeDirections()
    {
        var tradeDirections = Enum.GetNames(typeof(TradeDirection));

        return Task.FromResult<IActionResult>(new OkObjectResult(tradeDirections));
    }

    [HttpPost(Endpoints.Trade.Offer)]
    public async Task<IActionResult> Offer([FromBody] TradeOfferRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<TradeOfferRequest, CreateTradeOfferCommand>(request, (nameof(CreateTradeOfferCommand.SenderUserId), UserId));

        var result = await _mediator.Send(model, cancellationToken);

        return MapResult<TradeOfferResult, TradeOfferSuccessResponse, TradeOfferFailedResponse>(result);
    }

    [HttpPatch(Endpoints.Trade.Accept)]
    public async Task<IActionResult> Accept([FromBody] AcceptTradeOfferRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<AcceptTradeOfferRequest, RespondTradeCommand>(request, (nameof(RespondTradeCommand.UserId), UserId), (nameof(RespondTradeCommand.Response), true));

        var result = await _mediator.Send(model, cancellationToken);

        return MapResult<TradeOfferResult, AcceptTradeOfferSuccessResponse, AcceptTradeOfferFailedResponse>(result);
    }

    [HttpPatch(Endpoints.Trade.Reject)]
    public async Task<IActionResult> Reject([FromBody] RejectTradeOfferRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<RejectTradeOfferRequest, RespondTradeCommand>(request, (nameof(RespondTradeCommand.UserId), UserId), (nameof(RespondTradeCommand.Response), false));

        var result = await _mediator.Send(model, cancellationToken);

        return MapResult<TradeOfferResult, RejectTradeOfferSuccessResponse, RejectTradeOfferFailedResponse>(result);
    }

    [HttpDelete(Endpoints.Trade.Cancel)]
    public async Task<IActionResult> Cancel([FromBody] CancelTradeOfferRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<CancelTradeOfferRequest, CancelTradeCommand>(request, (nameof(RespondTradeCommand.UserId), UserId));

        var result = await _mediator.Send(model, cancellationToken);

        return MapResult<TradeOfferResult, CancelTradeOfferSuccessResponse, CancelTradeOfferFailedResponse>(result);
    }
}
