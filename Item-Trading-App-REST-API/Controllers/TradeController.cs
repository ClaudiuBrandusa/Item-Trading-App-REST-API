using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Item_Trading_App_Contracts.Responses.Trade;
using System.Linq;
using Item_Trading_App_Contracts.Base.Item;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Models.Item;

namespace Item_Trading_App_REST_API.Controllers;

[Authorize]
public class TradeController : BaseController
{
    private readonly ITradeService _tradeService;

    public TradeController(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    [HttpGet(Endpoints.Trade.GetSent)]
    public async Task<IActionResult> GetSent(string tradeId)
    {
        var result = await _tradeService.GetSentTradeOffer(new RequestTradeOffer
        {
            UserId = UserId,
            TradeOfferId = tradeId
        });

        if (result is null)
            return BadRequest(new GetSentTradeOfferFailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        if (!result.Success)
            return BadRequest(new GetSentTradeOfferFailedResponse
            {
                TradeOfferId = tradeId,
                Errors = result.Errors
            });

        return Ok(new GetSentTradeOfferSuccessResponse
        {
            TradeId = tradeId,
            ReceiverId = result.ReceiverId,
            ReceiverName = result.ReceiverName,
            Items = result.Items.Select(t => new ItemWithPrice { Id = t.ItemId, Name = t.Name, Price = t.Price, Quantity = t.Quantity }),
            SentDate = result.SentDate
        });
    }

    [HttpGet(Endpoints.Trade.GetSentResponded)]
    public async Task<IActionResult>  GetSentResponded(string tradeId)
    {
        var result = await _tradeService.GetSentRespondedTradeOffer(new RequestTradeOffer
        {
            UserId = UserId,
            TradeOfferId = tradeId
        });

        if (result is null)
            return BadRequest(new GetSentRespondedTradeOfferFailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        if (!result.Success)
            return BadRequest(new GetSentRespondedTradeOfferFailedResponse
            {
                TradeOfferId = tradeId,
                Errors = result.Errors
            });

        return Ok(new GetSentRespondedTradeOfferSuccessResponse
        {
            TradeId = tradeId,
            ReceiverId = result.ReceiverId,
            ReceiverName = result.ReceiverName,
            Items = result.Items.Select(t => new ItemWithPrice { Id = t.ItemId, Name = t.Name, Price = t.Price, Quantity = t.Quantity }),
            SentDate = result.SentDate,
            Response = result.Response,
            ResponseDate = result.ResponseDate
        });
    }

    [HttpGet(Endpoints.Trade.GetReceived)]
    public async Task<IActionResult> GetReceived(string tradeId)
    {
        var result = await _tradeService.GetReceivedTradeOffer(new RequestTradeOffer
        {
            UserId = UserId,
            TradeOfferId = tradeId
        });

        if (result is null)
            return BadRequest(new GetReceivedTradeOfferFailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        if (!result.Success)
            return BadRequest(new GetReceivedTradeOfferFailedResponse
            {
                TradeOfferId = tradeId,
                Errors = result.Errors
            });

        return Ok(new GetReceivedTradeOfferSuccessResponse
        {
            TradeId = tradeId,
            SenderId = result.SenderId,
            SenderName = result.SenderName,
            Items = result.Items.Select(t => new ItemWithPrice { Id = t.ItemId, Name = t.Name, Price = t.Price, Quantity = t.Quantity }),
            SentDate = result.SentDate
        });
    }

    [HttpGet(Endpoints.Trade.GetReceivedResponded)]
    public async Task<IActionResult> GetReceivedResponded(string tradeId)
    {
        var result = await _tradeService.GetReceivedRespondedTradeOffer(new RequestTradeOffer
        {
            UserId = UserId,
            TradeOfferId = tradeId
        });

        if (result is null)
            return BadRequest(new GetReceivedRespondedTradeOfferFailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        if (!result.Success)
            return BadRequest(new GetReceivedRespondedTradeOfferFailedResponse
            {
                TradeOfferId = tradeId,
                Errors = result.Errors
            });

        return Ok(new GetReceivedRespondedTradeOfferSuccessResponse
        {
            TradeId = tradeId,
            SenderId = result.SenderId,
            SenderName = result.SenderName,
            Items = result.Items.Select(t => new ItemWithPrice { Id = t.ItemId, Name = t.Name, Price = t.Price, Quantity = t.Quantity }),
            SentDate = result.SentDate,
            Response = result.Response,
            ResponseDate = result.ResponseDate
        });
    }

    [HttpGet(Endpoints.Trade.ListSent)]
    public async Task<IActionResult> ListSent()
    {
        var results = await _tradeService.GetSentTradeOffers(UserId);

        if (results is null)
            return BadRequest(new FailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        if (!results.Success)
            return BadRequest(new FailedResponse
            {
                Errors = results.Errors
            });

        return Ok(new ListTradeOffersSuccessResponse
        {
            TradeOffersIds = results.TradeOffers
        });
    }

    [HttpGet(Endpoints.Trade.ListSentResponded)]
    public async Task<IActionResult> ListSentResponded()
    {
        var results = await _tradeService.GetSentRespondedTradeOffers(UserId);

        if (results is null)
            return BadRequest(new FailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        if (!results.Success)
            return BadRequest(new FailedResponse
            {
                Errors = results.Errors
            });

        return Ok(new ListTradeOffersSuccessResponse
        {
            TradeOffersIds = results.TradeOffers
        });
    }

    [HttpGet(Endpoints.Trade.ListReceived)]
    public async Task<IActionResult> ListReceived()
    {
        var results = await _tradeService.GetReceivedTradeOffers(UserId);

        if (results is null)
            return BadRequest(new FailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        if (!results.Success)
            return BadRequest(new FailedResponse
            {
                Errors = results.Errors
            });

        return Ok(new ListTradeOffersSuccessResponse
        {
            TradeOffersIds = results.TradeOffers
        });
    }

    [HttpGet(Endpoints.Trade.ListReceivedResponded)]
    public async Task<IActionResult> ListReceivedResponded()
    {
        var results = await _tradeService.GetReceivedRespondedTradeOffers(UserId);

        if (results is null)
            return BadRequest(new FailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        if (!results.Success)
            return BadRequest(new FailedResponse
            {
                Errors = results.Errors
            });

        return Ok(new ListTradeOffersSuccessResponse
        {
            TradeOffersIds = results.TradeOffers
        });
    }

    [HttpPost(Endpoints.Trade.Offer)]
    public async Task<IActionResult> Offer([FromBody] TradeOfferRequest request)
    {
        if (request is null || string.IsNullOrEmpty(request.TargetUserId) || request.Items is null || !request.Items.Any())
            return BadRequest(new GetSentTradeOfferFailedResponse
            {
                Errors = new[] { "Invalid input data" }
            });

        var result = await _tradeService.CreateTradeOffer(new CreateTradeOffer
        {
            SenderUserId = UserId,
            TargetUserId = request.TargetUserId,
            Items = request.Items.Select(t => new ItemPrice { ItemId = t.Id, Price = t.Price, Quantity = t.Quantity })
        });

        if (result is null)
            return BadRequest(new GetSentTradeOfferFailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        if (!result.Success)
            return BadRequest(new GetSentTradeOfferFailedResponse
            {
                Errors = result.Errors
            });

        return Ok(new GetSentTradeOfferSuccessResponse
        {
            TradeId = result.TradeOfferId,
            ReceiverId = result.ReceiverId,
            ReceiverName = result.ReceiverName,
            SentDate = result.SentDate,
            Items = result.Items.Select(t => new ItemWithPrice { Id = t.ItemId, Name = t.Name, Price = t.Price, Quantity = t.Quantity})
        });
    }

    [HttpPatch(Endpoints.Trade.Accept)]
    public async Task<IActionResult> Accept([FromBody] AcceptTradeOfferRequest request)
    {
        if (request is null || string.IsNullOrEmpty(request.TradeId))
            return BadRequest(new AcceptTradeOfferFailedResponse
            {
                Errors = new[] { "Invalid input data" }
            });

        var result = await _tradeService.AcceptTradeOffer(request.TradeId, UserId);

        if (result is null)
            return BadRequest(new AcceptTradeOfferFailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        if (!result.Success)
            return BadRequest(new AcceptTradeOfferFailedResponse
            {
                Errors = result.Errors
            });

        return Ok(new AcceptTradeOfferSuccessResponse
        {
            TradeOfferId = result.TradeOfferId,
            SenderId = result.SenderId,
            SenderName = result.SenderName,
            ReceivedDate = result.ReceivedDate,
            ResponseDate = result.ResponseDate
        });
    }

    [HttpPatch(Endpoints.Trade.Reject)]
    public async Task<IActionResult> Reject([FromBody] RejectTradeOfferRequest request)
    {
        if (request is null || string.IsNullOrEmpty(request.TradeId))
            return BadRequest(new RejectTradeOfferFailedResponse
            {
                Errors = new[] { "Invalid input data" }
            });

        var result = await _tradeService.RejectTradeOffer(request.TradeId, UserId);

        if (result is null)
            return BadRequest(new RejectTradeOfferFailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        if (!result.Success)
            return BadRequest(new RejectTradeOfferFailedResponse
            {
                Errors = result.Errors
            });

        return Ok(new RejectTradeOfferSuccessResponse
        {
            Id = result.TradeOfferId,
            SenderId = result.SenderId,
            SenderName = result.SenderName,
            ReceivedDate = result.ReceivedDate,
            ResponseDate = result.ResponseDate
        });
    }

    [HttpDelete(Endpoints.Trade.Cancel)]
    public async Task<IActionResult> Cancel([FromBody] CancelTradeOfferRequest request)
    {
        if (request is null || string.IsNullOrEmpty(request.TradeId))
            return BadRequest(new CancelTradeOfferFailedResponse
            {
                Errors = new[] { "Invalid input data" }
            });

        var result = await _tradeService.CancelTradeOffer(request.TradeId, UserId);

        if (result is null)
            return BadRequest(new CancelTradeOfferFailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        if (!result.Success)
            return BadRequest(new CancelTradeOfferFailedResponse
            {
                Errors = result.Errors
            });

        return Ok(new CancelTradeOfferSuccessResponse
        {
            TradeOfferId = result.TradeOfferId,
            ReceiverId = result.ReceiverId,
            ReceiverName = result.ReceiverName
        });
    }
}
