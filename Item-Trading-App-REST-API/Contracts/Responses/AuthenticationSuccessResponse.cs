namespace Item_Trading_App_REST_API.Contracts.Responses
{
    public class AuthenticationSuccessResponse
    {
        public string Token { get; set; }

        public string RefreshToken { get; set; }
    }
}
