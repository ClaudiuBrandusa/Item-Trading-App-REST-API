namespace Application.Results.RefreshToken;

public record RefreshTokenResult
{
    public string Token { get; set; } = string.Empty;

    public string JwtId { get; set; } = string.Empty;

    public DateTime CreationDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public bool Used { get; set; }

    public bool Invalidated { get; set; }

    public string UserId { get; set; } = string.Empty;
}
