using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Trade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Item_Trading_App_REST_API.Controllers
{
    [Authorize]
    public class TradeController : Controller
    {
        [HttpGet(Endpoints.Trade.Get)]
        public IActionResult Get(string tradeId)
        {
            return Ok();
        }

        [HttpGet(Endpoints.Trade.List)]
        public IActionResult List()
        {
            return Ok();
        }

        [HttpPost(Endpoints.Trade.Offer)]
        public IActionResult Offer([FromBody] TradeOfferRequest request)
        {
            return Ok();
        }

        [HttpPatch(Endpoints.Trade.Accept)]
        public IActionResult Accept([FromBody] AcceptTradeOfferRequest request)
        {
            return Ok();
        }

        [HttpPatch(Endpoints.Trade.Reject)]
        public IActionResult Reject([FromBody] RejectTradeOfferRequest request)
        {
            return Ok();
        }

        [HttpDelete(Endpoints.Trade.Cancel)]
        public IActionResult Cancel([FromBody] CancelTradeOfferRequest request)
        {
            return Ok();
        }
    }
}
