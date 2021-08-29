using Item_Trading_App_Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Item_Trading_App_REST_API.Controllers
{
    [Authorize]
    public class WalletController : Controller
    {
        [HttpGet(Endpoints.Wallet.Get)]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpPatch(Endpoints.Wallet.Update)]
        public IActionResult Update()
        {
            return Ok();
        }
    }
}
