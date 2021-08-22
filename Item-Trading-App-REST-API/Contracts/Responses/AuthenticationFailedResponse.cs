using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Contracts.Responses
{
    public class AuthenticationFailedResponse
    {
        public IEnumerable<string> Errors { get; set; }
    }
}
