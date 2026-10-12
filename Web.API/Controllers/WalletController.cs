using Application.Behaviors.Wallet.GetWallet;
using Application.Behaviors.Wallet.UpdateWallet;
using Application.Results.Wallet;
using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Wallet;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Wallet;
using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers;

[Authorize]
public class WalletController : BaseController
{
    private readonly IMediator _mediator;

    public WalletController(ILogger<WalletController> logger, IMapper mapper, IMediator mediator) : base(logger, mapper)
    {
        _mediator = mediator;
    }

    [HttpGet(Endpoints.Wallet.Get)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var model = AdaptToType<string, GetUserWalletQuery>(UserId);

        var result = await _mediator.Send(model, cancellationToken);

        return MapResult<WalletResult, WalletSuccessResponse, FailedResponse>(result);
    }

    [HttpPatch(Endpoints.Wallet.Update)]
    public async Task<IActionResult> Update([FromBody] UpdateWalletRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new FailedResponse
            {
                Errors = ["Something went wrong"]
            });

        var model = AdaptToType<UpdateWalletRequest, UpdateWalletCommand>(request, (nameof(UpdateWalletCommand.UserId), UserId));

        var result = await _mediator.Send(model, cancellationToken);

        return MapResult<WalletResult, UpdateWalletSuccessResponse, FailedResponse>(result);
    }
}
