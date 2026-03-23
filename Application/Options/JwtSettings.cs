namespace Application.Options;

public record JwtSettings
{
    public string Secret { get; set; } = string.Empty;

    public TimeSpan TokenLifetime { get; set; }

    public TimeSpan RefreshTokenLifetime { get; set; }

    public int AllowedRefreshTokensPerUser { get; set; }
}
