using Item_Trading_App_REST_API.Models.Base;
using System;

namespace Item_Trading_App_REST_API.Models.Identity;

public record AuthenticationResult : BaseResult
{
    public string Token { get; set; }

    public string RefreshToken { get; set; }

    public DateTime ExpirationDateTime { get; set; }
}
