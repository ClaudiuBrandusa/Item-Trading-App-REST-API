using Item_Trading_App_REST_API.Contracts.Requests;
using Item_Trading_App_REST_API.Contracts.Responses;
using Item_Trading_App_REST_API.Services.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers
{
    public class IdentityController : Controller
    {
        private readonly IIdentityService _identityService;

        public IdentityController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [HttpPost("/identity/register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new AuthenticationFailedResponse
                {
                    Errors = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage))
                });

            var authResponse = await _identityService.RegisterAsync(request.Username, request.Password);

            if (!authResponse.Success)
                return BadRequest(new AuthenticationFailedResponse
                {
                    Errors = authResponse.Errors
                });

            return Ok(new AuthenticationSuccessResponse
            {
                Token = authResponse.Token
            });
        }

        [HttpPost("/identity/login")]
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

            return Ok(new AuthenticationSuccessResponse
            {
                Token = authResponse.Token
            });
        }
    }
}
