using System;

namespace Item_Trading_App_REST_API.Options;

public record JwtSettings
{
    public string Secret { get; set; }

    public TimeSpan TokenLifetime { get; set; }

    public TimeSpan RefreshTokenLifetime { get; set; }

    public int AllowedRefreshTokensPerUser { get; set; }
}
