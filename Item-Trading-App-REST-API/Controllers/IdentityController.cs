using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Identity;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Identity;
using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Services.Identity;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers;

public class IdentityController : BaseController
{
    private readonly IIdentityService _identityService;

    public IdentityController(IIdentityService identityService, IMapper mapper) : base(mapper)
    {
        _identityService = identityService;
    }

    [HttpPost(Endpoints.Identity.Register)]
    public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new AuthenticationFailedResponse { Errors = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)) });

        var result = await _identityService.RegisterAsync(AdaptToType<UserRegisterRequest, Register>(request));

        return MapResult<AuthenticationResult, AuthenticationSuccessResponse, AuthenticationFailedResponse>(result);
    }

    [HttpPost(Endpoints.Identity.Login)]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new AuthenticationFailedResponse { Errors = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)) });

        var result = await _identityService.LoginAsync(AdaptToType<UserLoginRequest, Login>(request));

        return MapResult<AuthenticationResult, AuthenticationSuccessResponse, AuthenticationFailedResponse>(result);
    }

    [HttpPost(Endpoints.Identity.Refresh)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new AuthenticationFailedResponse { Errors = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)) });

        var result = await _identityService.RefreshTokenAsync(AdaptToType<RefreshTokenRequest, RefreshTokenData>(request));

        return MapResult<AuthenticationResult, AuthenticationSuccessResponse, AuthenticationFailedResponse>(result);
    }

    [Authorize]
    [HttpGet(Endpoints.Identity.GetUsername)]
    public async Task<IActionResult> GetUsername(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return BadRequest(new FailedResponse
            {
                Errors = new[] { "Invalid user id" }
            });

        return Ok(AdaptToType<string, UsernameSuccessResponse>(userId, ("username", await _identityService.GetUsername(UserId))));
    }

    [Authorize]
    [HttpGet(Endpoints.Identity.ListUsers)]
    public async Task<IActionResult> ListUsers(string searchString)
    {
        var result = await _identityService.ListUsers(AdaptToType<string, ListUsers>(searchString, ("userId", UserId)));

        return MapResult<UsersResult, UsersSuccessResponse, FailedResponse>(result);
    }
}
