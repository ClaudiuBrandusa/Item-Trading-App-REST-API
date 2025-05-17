using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Application.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetJwtId(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
    }
}
