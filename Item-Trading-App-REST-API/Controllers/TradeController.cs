using Item_Trading_App_Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Item_Trading_App_REST_API.Controllers
{
    [Authorize]
    public class TradeController : Controller
    {
        [HttpGet(Endpoints.Trade.Get)]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpGet(Endpoints.Trade.GetAll)]
        public IActionResult GetAll()
        {
            return Ok();
        }

        [HttpPost(Endpoints.Trade.Create)]
        public IActionResult Create()
        {
            return Ok();
        }

        [HttpPatch(Endpoints.Trade.Accept)]
        public IActionResult Accept()
        {
            return Ok();
        }

        [HttpPatch(Endpoints.Trade.Reject)]
        public IActionResult Reject()
        {
            return Ok();
        }

        [HttpDelete(Endpoints.Trade.Cancel)]
        public IActionResult Cancel()
        {
            return Ok();
        }
    }
}
