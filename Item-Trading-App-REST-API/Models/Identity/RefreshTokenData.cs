﻿namespace Item_Trading_App_REST_API.Models.Identity;

public record RefreshTokenData
{
    public string Token { get; set; }

    public string RefreshToken { get; set; }
}