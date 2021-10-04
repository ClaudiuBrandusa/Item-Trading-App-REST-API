using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Wallet;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers
{
    [Authorize]
    public class WalletController : BaseController
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet(Endpoints.Wallet.Get)]
        public async Task<IActionResult> Get()
        {
            string userId = UserId;

            if(string.IsNullOrEmpty(userId))
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "User id not found" }
                });
            }

            var wallet = await _walletService.GetWalletAsync(userId);

            if(wallet == null)
            {
                return BadRequest(new FailedResponse 
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            if(!wallet.Success)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = wallet.Errors
                });
            }

            return Ok(new WalletSuccessResponse
            {
                Cash = wallet.Cash
            });
        }

        [HttpPatch(Endpoints.Wallet.Update)]
        public async Task<IActionResult> Update([FromBody] UpdateWalletRequest request)
        {
            var userId = UserId;

            if (string.IsNullOrEmpty(userId) || request == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            var wallet = await _walletService.UpdateWalletAsync(userId, request.Quantity);

            if(!wallet.Success)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = wallet.Errors
                });
            }

            return Ok(new UpdateWalletSuccessResponse
            {
                Amount = wallet.Cash
            });
        }
    }
}
