using System;

namespace Item_Trading_App_REST_API.Options;

public record RefreshTokenSettings
{
    public TimeSpan ClearRefreshTokenInterval { get; set; }
}
