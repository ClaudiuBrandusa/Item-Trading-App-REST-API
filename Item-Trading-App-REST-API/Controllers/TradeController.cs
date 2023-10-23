using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Item_Trading_App_Contracts.Responses.Trade;
using System.Linq;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_REST_API.Models.Trade;
using MapsterMapper;

namespace Item_Trading_App_REST_API.Controllers;

[Authorize]
public class TradeController : BaseController
{
    private readonly ITradeService _tradeService;

    public TradeController(ITradeService tradeService, IMapper mapper) : base(mapper)
    {
        _tradeService = tradeService;
    }

    [HttpGet(Endpoints.Trade.GetSent)]
    public async Task<IActionResult> GetSent(string tradeId)
    {
        var result = await _tradeService.GetSentTradeOffer(AdaptToType<string, RequestTradeOffer>(tradeId, ("userId", UserId)));

        return MapResult<SentTradeOffer, GetSentTradeOfferSuccessResponse, GetSentTradeOfferFailedResponse>(result);
    }

    [HttpGet(Endpoints.Trade.GetSentResponded)]
    public async Task<IActionResult> GetSentResponded(string tradeId)
    {
        var result = await _tradeService.GetSentRespondedTradeOffer(AdaptToType<string, RequestTradeOffer>(tradeId, ("userId", UserId)));

        return MapResult<SentRespondedTradeOffer, GetSentRespondedTradeOfferSuccessResponse, GetSentRespondedTradeOfferFailedResponse>(result);
    }

    [HttpGet(Endpoints.Trade.GetReceived)]
    public async Task<IActionResult> GetReceived(string tradeId)
    {
        var result = await _tradeService.GetReceivedTradeOffer(AdaptToType<string, RequestTradeOffer>(tradeId, ("userId", UserId)));

        return MapResult<ReceivedTradeOffer, GetReceivedTradeOfferSuccessResponse, GetReceivedTradeOfferFailedResponse>(result);
    }

    [HttpGet(Endpoints.Trade.GetReceivedResponded)]
    public async Task<IActionResult> GetReceivedResponded(string tradeId)
    {
        var result = await _tradeService.GetReceivedRespondedTradeOffer(AdaptToType<string, RequestTradeOffer>(tradeId, ("userId", UserId)));

        return MapResult<ReceivedRespondedTradeOffer, GetReceivedRespondedTradeOfferSuccessResponse, GetReceivedRespondedTradeOfferFailedResponse>(result);
    }

    [HttpGet(Endpoints.Trade.ListSent)]
    public async Task<IActionResult> ListSent()
    {
        var results = await _tradeService.GetSentTradeOffers(UserId);

        return MapResult<TradeOffersResult, ListTradeOffersSuccessResponse, FailedResponse>(results);
    }

    [HttpGet(Endpoints.Trade.ListSentResponded)]
    public async Task<IActionResult> ListSentResponded()
    {
        var results = await _tradeService.GetSentRespondedTradeOffers(UserId);

        return MapResult<TradeOffersResult, ListTradeOffersSuccessResponse, FailedResponse>(results);
    }

    [HttpGet(Endpoints.Trade.ListReceived)]
    public async Task<IActionResult> ListReceived()
    {
        var results = await _tradeService.GetReceivedTradeOffers(UserId);

        return MapResult<TradeOffersResult, ListTradeOffersSuccessResponse, FailedResponse>(results);
    }

    [HttpGet(Endpoints.Trade.ListReceivedResponded)]
    public async Task<IActionResult> ListReceivedResponded()
    {
        var results = await _tradeService.GetReceivedRespondedTradeOffers(UserId);

        return MapResult<TradeOffersResult, ListTradeOffersSuccessResponse, FailedResponse>(results);
    }

    [HttpPost(Endpoints.Trade.Offer)]
    public async Task<IActionResult> Offer([FromBody] TradeOfferRequest request)
    {
        if (request is null || string.IsNullOrEmpty(request.TargetUserId) || request.Items is null || !request.Items.Any())
            return BadRequest(new GetSentTradeOfferFailedResponse
            {
                Errors = new[] { "Invalid input data" }
            });

        var result = await _tradeService.CreateTradeOffer(AdaptToType<TradeOfferRequest, CreateTradeOffer>(request, ("userId", UserId)));

        return MapResult<SentTradeOffer, GetSentTradeOfferSuccessResponse, GetSentTradeOfferFailedResponse>(result);
    }

    [HttpPatch(Endpoints.Trade.Accept)]
    public async Task<IActionResult> Accept([FromBody] AcceptTradeOfferRequest request)
    {
        if (request is null || string.IsNullOrEmpty(request.TradeId))
            return BadRequest(new AcceptTradeOfferFailedResponse
            {
                Errors = new[] { "Invalid input data" }
            });

        var result = await _tradeService.AcceptTradeOffer(AdaptToType<AcceptTradeOfferRequest, RespondTrade>(request, ("userId", UserId)));

        return MapResult<AcceptTradeOfferResult, AcceptTradeOfferSuccessResponse, AcceptTradeOfferFailedResponse>(result);
    }

    [HttpPatch(Endpoints.Trade.Reject)]
    public async Task<IActionResult> Reject([FromBody] RejectTradeOfferRequest request)
    {
        if (request is null || string.IsNullOrEmpty(request.TradeId))
            return BadRequest(new RejectTradeOfferFailedResponse
            {
                Errors = new[] { "Invalid input data" }
            });

        var result = await _tradeService.RejectTradeOffer(AdaptToType<RejectTradeOfferRequest, RespondTrade>(request, ("userId", UserId)));

        return MapResult<RejectTradeOfferResult, RejectTradeOfferSuccessResponse, RejectTradeOfferFailedResponse>(result);
    }

    [HttpDelete(Endpoints.Trade.Cancel)]
    public async Task<IActionResult> Cancel([FromBody] CancelTradeOfferRequest request)
    {
        if (request is null || string.IsNullOrEmpty(request.TradeId))
            return BadRequest(new CancelTradeOfferFailedResponse
            {
                Errors = new[] { "Invalid input data" }
            });

        var result = await _tradeService.CancelTradeOffer(AdaptToType<CancelTradeOfferRequest, RespondTrade>(request, ("userId", UserId)));

        return MapResult<CancelTradeOfferResult, CancelTradeOfferSuccessResponse, CancelTradeOfferFailedResponse>(result);
    }
}
