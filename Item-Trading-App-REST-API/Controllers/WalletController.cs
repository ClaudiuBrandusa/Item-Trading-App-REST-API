using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Wallet;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Wallet;
using Item_Trading_App_REST_API.Models.Wallet;
using Item_Trading_App_REST_API.Resources.Commands.Wallet;
using Item_Trading_App_REST_API.Resources.Queries.Wallet;
using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers;

[Authorize]
public class WalletController : BaseController
{
    private readonly IMediator _mediator;

    public WalletController(IMapper mapper, IMediator mediator) : base(mapper)
    {
        _mediator = mediator;
    }

    [HttpGet(Endpoints.Wallet.Get)]
    public async Task<IActionResult> Get()
    {
        var model = AdaptToType<string, GetUserWalletQuery>(UserId);

        var result = await _mediator.Send(model);

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

        var model = AdaptToType<UpdateWalletRequest, UpdateWalletCommand>(request, (nameof(UpdateWalletCommand.UserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<WalletResult, UpdateWalletSuccessResponse, FailedResponse>(result);
    }
}
