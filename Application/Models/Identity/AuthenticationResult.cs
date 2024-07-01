using Application.Models.Base;

namespace Application.Models.Identity;

public record AuthenticationResult : BaseResult
{
    public string Token { get; set; }

    public string RefreshToken { get; set; }

    public DateTime ExpirationDateTime { get; set; }
}
