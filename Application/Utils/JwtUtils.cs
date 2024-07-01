using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Application.Utils;
public static class JwtUtils
{
    public static TokenValidationParameters BuildTokenValidationParameters(string secret)
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            RequireExpirationTime = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }
}
