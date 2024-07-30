using Application.Models;

namespace Application.Results.Identity;

public record AuthenticationResult : Result
{
    public string Token { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime ExpirationDateTime { get; set; }
}
