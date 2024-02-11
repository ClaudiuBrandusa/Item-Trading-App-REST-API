using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Trade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Item_Trading_App_Contracts.Responses.Trade;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_REST_API.Models.Trade;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MediatR;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Resources.Commands.Trade;

namespace Item_Trading_App_REST_API.Controllers;

[Authorize]
public class TradeController : BaseController
{
    private readonly IMediator _mediator;

    public TradeController(IMapper mapper, IMediator mediator) : base(mapper)
    {
        _mediator = mediator;
    }

    [HttpGet(Endpoints.Trade.GetSent)]
    public async Task<IActionResult> GetSent(string tradeId)
    {
        var model = AdaptToType<string, RequestSentTradeOfferQuery>(tradeId, (nameof(RequestTradeOfferQuery.UserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<SentTradeOfferResult, GetSentTradeOfferSuccessResponse, GetSentTradeOfferFailedResponse>(result);
    }

    [HttpGet(Endpoints.Trade.GetSentResponded)]
    public async Task<IActionResult> GetSentResponded(string tradeId)
    {
        var model = AdaptToType<string, RequestRespondedSentTradeOfferQuery>(tradeId, (nameof(RequestTradeOfferQuery.UserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<RespondedSentTradeOfferResult, GetSentRespondedTradeOfferSuccessResponse, GetSentRespondedTradeOfferFailedResponse>(result);
    }

    [HttpGet(Endpoints.Trade.GetReceived)]
    public async Task<IActionResult> GetReceived(string tradeId)
    {
        var model = AdaptToType<string, RequestReceivedTradeOfferQuery>(tradeId, (nameof(RequestTradeOfferQuery.UserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<ReceivedTradeOfferResult, GetReceivedTradeOfferSuccessResponse, GetReceivedTradeOfferFailedResponse>(result);
    }

    [HttpGet(Endpoints.Trade.GetReceivedResponded)]
    public async Task<IActionResult> GetReceivedResponded(string tradeId)
    {
        var model = AdaptToType<string, RequestRespondedReceivedTradeOfferQuery>(tradeId, (nameof(RequestTradeOfferQuery.UserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<RespondedReceivedTradeOfferResult, GetReceivedRespondedTradeOfferSuccessResponse, GetReceivedRespondedTradeOfferFailedResponse>(result);
    }

    [HttpGet(Endpoints.Trade.ListSent)]
    public async Task<IActionResult> ListSent([FromQuery] string[] tradeItemIds)
    {
        var model = AdaptToType<string, ListSentTradesQuery>(UserId, (nameof(ListTradesQuery.TradeItemIds), tradeItemIds));

        var results = await _mediator.Send(model);

        return MapResult<TradeOffersResult, ListTradeOffersSuccessResponse, FailedResponse>(results);
    }

    [HttpGet(Endpoints.Trade.ListSentResponded)]
    public async Task<IActionResult> ListSentResponded([FromQuery] string[] tradeItemIds)
    {
        var model = AdaptToType<string, ListRespondedSentTradesQuery>(UserId, (nameof(ListTradesQuery.TradeItemIds), tradeItemIds));

        var results = await _mediator.Send(model);

        return MapResult<TradeOffersResult, ListTradeOffersSuccessResponse, FailedResponse>(results);
    }

    [HttpGet(Endpoints.Trade.ListReceived)]
    public async Task<IActionResult> ListReceived([FromQuery] string[] tradeItemIds)
    {
        var model = AdaptToType<string, ListReceivedTradesQuery>(UserId, (nameof(ListTradesQuery.TradeItemIds), tradeItemIds));

        var results = await _mediator.Send(model);

        return MapResult<TradeOffersResult, ListTradeOffersSuccessResponse, FailedResponse>(results);
    }

    [HttpGet(Endpoints.Trade.ListReceivedResponded)]
    public async Task<IActionResult> ListReceivedResponded([FromQuery] string[] tradeItemIds)
    {
        var model = AdaptToType<string, ListRespondedReceivedTradesQuery>(UserId, (nameof(ListTradesQuery.TradeItemIds), tradeItemIds));

        var results = await _mediator.Send(model);

        return MapResult<TradeOffersResult, ListTradeOffersSuccessResponse, FailedResponse>(results);
    }

    [HttpPost(Endpoints.Trade.Offer)]
    public async Task<IActionResult> Offer([FromBody] TradeOfferRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<TradeOfferRequest, CreateTradeOfferCommand>(request, (nameof(CreateTradeOfferCommand.SenderUserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<SentTradeOfferResult, GetSentTradeOfferSuccessResponse, GetSentTradeOfferFailedResponse>(result);
    }

    [HttpPatch(Endpoints.Trade.Accept)]
    public async Task<IActionResult> Accept([FromBody] AcceptTradeOfferRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<AcceptTradeOfferRequest, RespondTradeCommand>(request, (nameof(RespondTradeCommand.UserId), UserId), (nameof(RespondTradeCommand.Response), true));

        var result = await _mediator.Send(model);

        return MapResult<RespondedTradeOfferResult, AcceptTradeOfferSuccessResponse, AcceptTradeOfferFailedResponse>(result);
    }

    [HttpPatch(Endpoints.Trade.Reject)]
    public async Task<IActionResult> Reject([FromBody] RejectTradeOfferRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<RejectTradeOfferRequest, RespondTradeCommand>(request, (nameof(RespondTradeCommand.UserId), UserId), (nameof(RespondTradeCommand.Response), false));

        var result = await _mediator.Send(model);

        return MapResult<RespondedTradeOfferResult, RejectTradeOfferSuccessResponse, RejectTradeOfferFailedResponse>(result);
    }

    [HttpDelete(Endpoints.Trade.Cancel)]
    public async Task<IActionResult> Cancel([FromBody] CancelTradeOfferRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<CancelTradeOfferRequest, CancelTradeCommand>(request, (nameof(RespondTradeCommand.UserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<CancelTradeOfferResult, CancelTradeOfferSuccessResponse, CancelTradeOfferFailedResponse>(result);
    }
}
