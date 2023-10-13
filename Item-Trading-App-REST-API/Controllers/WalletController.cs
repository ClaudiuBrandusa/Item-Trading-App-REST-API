using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Wallet;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Wallet;
using Item_Trading_App_REST_API.Models.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers;

[Authorize]
public class WalletController : BaseController
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService, IMapper mapper) : base(mapper)
    {
        _walletService = walletService;
    }

    [HttpGet(Endpoints.Wallet.Get)]
    public async Task<IActionResult> Get()
    {
        var result = await _walletService.GetWalletAsync(UserId);

        return MapResult<WalletResult, WalletSuccessResponse, FailedResponse>(result);
    }

    [HttpPatch(Endpoints.Wallet.Update)]
    public async Task<IActionResult> Update([FromBody] UpdateWalletRequest request)
    {
        if (request is null)
            return BadRequest(new FailedResponse
            {
                Errors = new[] { "Something went wrong" }
            });

        var result = await _walletService.UpdateWalletAsync(AdaptToType<UpdateWalletRequest, UpdateWallet>(request, ("userId", UserId)));

        return MapResult<WalletResult, UpdateWalletSuccessResponse, FailedResponse>(result);
    }
}
