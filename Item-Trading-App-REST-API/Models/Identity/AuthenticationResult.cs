using Item_Trading_App_REST_API.Models.Base;

namespace Item_Trading_App_REST_API.Models.Identity
{
    public class AuthenticationResult : BaseResult
    {
        public string Token { get; set; }

        public string RefreshToken { get; set; }
    }
}
