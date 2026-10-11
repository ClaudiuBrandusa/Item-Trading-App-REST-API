using Application.Behaviors.Identity.GetUsername;
using Application.Behaviors.Identity.ListUsers;
using Application.Behaviors.Identity.LoginUser;
using Application.Behaviors.Identity.RefreshToken;
using Application.Behaviors.Identity.RegisterUser;
using Application.Results.Identity;
using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Identity;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Identity;
using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers;

public class IdentityController : BaseController
{
    private readonly IMediator _mediator;

    public IdentityController(IMapper mapper, IMediator mediator) : base(mapper)
    {
        _mediator = mediator;
    }

    [HttpPost(Endpoints.Identity.Register)]
    public async Task<IActionResult> Register([FromBody] UserRegisterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, AuthenticationFailedResponse>(ModelState));

        var model = AdaptToType<UserRegisterRequest, RegisterCommand>(request);

        var result = await _mediator.Send(model, cancellationToken);

        return MapResult<AuthenticationResult, AuthenticationSuccessResponse, AuthenticationFailedResponse>(result);
    }

    [HttpPost(Endpoints.Identity.Login)]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, AuthenticationFailedResponse>(ModelState));

        var model = AdaptToType<UserLoginRequest, LoginCommand>(request);

        var result = await _mediator.Send(model, cancellationToken);

        return MapResult<AuthenticationResult, AuthenticationSuccessResponse, AuthenticationFailedResponse>(result);
    }

    [HttpPost(Endpoints.Identity.Refresh)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, AuthenticationFailedResponse>(ModelState));

        var model = AdaptToType<RefreshTokenRequest, RefreshTokenCommand>(request);

        var result = await _mediator.Send(model, cancellationToken);

        return MapResult<AuthenticationResult, AuthenticationSuccessResponse, AuthenticationFailedResponse>(result);
    }

    [Authorize]
    [HttpGet(Endpoints.Identity.GetUsername)]
    public async Task<IActionResult> GetUsername(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId))
            return BadRequest(new FailedResponse
            {
                Errors = ["Invalid user id"]
            });

        var result = await _mediator.Send(new GetUsernameQuery { UserId = userId }, cancellationToken);

        return MapResult<string, UsernameSuccessResponse, FailedResponse>(result, (nameof(UsernameSuccessResponse.UserId), userId));
    }

    [Authorize]
    [HttpGet(Endpoints.Identity.ListUsers)]
    public async Task<IActionResult> ListUsers(string searchString, CancellationToken cancellationToken)
    {
        var model = AdaptToType<string, ListUsersQuery>(searchString, (nameof(ListUsersQuery.UserId), UserId));

        var result = await _mediator.Send(model, cancellationToken);

        return MapResult<UsersResult, UsersSuccessResponse, FailedResponse>(result);
    }
}
