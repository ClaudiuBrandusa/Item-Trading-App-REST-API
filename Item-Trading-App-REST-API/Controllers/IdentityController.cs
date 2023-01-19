using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Identity;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Identity;
using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers
{
    public class IdentityController : BaseController
    {
        private readonly IIdentityService _identityService;

        public IdentityController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [HttpPost(Endpoints.Identity.Register)]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new AuthenticationFailedResponse { Errors = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)) });

            var authResponse = await _identityService.RegisterAsync(request.Username, request.Password);

            if (!authResponse.Success)
                return BadRequest(new AuthenticationFailedResponse
                {
                    Errors = authResponse.Errors
                });

            return Ok(ReturnSuccessResponse(authResponse));
        }

        [HttpPost(Endpoints.Identity.Login)]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new AuthenticationFailedResponse { Errors = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)) });

            var authResponse = await _identityService.LoginAsync(request.Username, request.Password);

            if (!authResponse.Success)
                return BadRequest(new AuthenticationFailedResponse
                {
                    Errors = authResponse.Errors
                });

            return Ok(ReturnSuccessResponse(authResponse));
        }

        [HttpPost(Endpoints.Identity.Refresh)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new AuthenticationFailedResponse { Errors = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)) });

            var authResponse = await _identityService.RefreshTokenAsync(request.Token, request.RefreshToken);

            if (!authResponse.Success)
                return BadRequest(new AuthenticationFailedResponse
                {
                    Errors = authResponse.Errors
                });

            return Ok(ReturnSuccessResponse(authResponse));
        }

        [Authorize]
        [HttpGet(Endpoints.Identity.GetUsername)]
        public async Task<IActionResult> GetUsername(string userId)
        {
            if(string.IsNullOrEmpty(userId))
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Invalid user id" }
                });
            }

            return Ok(new UsernameSuccessResponse
            {
                UserId = userId,
                Username = await _identityService.GetUsername(userId)
            });
        }

        [Authorize]
        [HttpGet(Endpoints.Identity.ListUsers)]
        public async Task<IActionResult> ListUsers(string searchString)
        {
            var result = await _identityService.ListUsers(UserId, searchString);


            if (result == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            if(!result.Success)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = result.Errors
                });
            }

            return Ok(new UsersSuccessResponse
            {
                UsersId = result.UsersId
            });
        }

        private AuthenticationSuccessResponse ReturnSuccessResponse(AuthenticationResult result) =>
            new AuthenticationSuccessResponse
            {
                Token = result.Token,
                RefreshToken = result.RefreshToken
            };
    }
}
