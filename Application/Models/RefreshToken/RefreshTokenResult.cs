using Application.Models.Base;

namespace Application.Models.RefreshToken;

public record RefreshTokenResult : BaseResult
{
    public string Token { get; set; }

    public string JwtId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public bool Used { get; set; }

    public bool Invalidated { get; set; }

    public string UserId { get; set; }
}
