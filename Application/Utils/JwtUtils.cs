using Domain.Entities.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Utils;

public static class JwtUtils
{
    public static TokenValidationParameters BuildTokenValidationParameters(string secret)
    {
        string formattedSecret = FormatSecretKey(secret);

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(formattedSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            RequireExpirationTime = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    public static SecurityTokenDescriptor CreateSecurityTokenDescriptor(List<Claim> claims, DateTime expirationTime, SigningCredentials signingCredentials)
    {
        return new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expirationTime,
            SigningCredentials = signingCredentials
        };
    }

    public static List<Claim> CreateUserJwtClaims(User user)
    {
        return CreateUserJwtClaims(user.Id, user.UserName!);
    }

    public static List<Claim> CreateUserJwtClaims(string userId, string userName)
    {
        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, userName),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new ("id", userId),
        };

        return claims;
    }

    /// <summary>
    /// Creates the SigningCredentials from <paramref name="key"/> using the HMAC Sha256 algorithm
    /// </summary>
    public static SigningCredentials CreateSigningCredentials(byte[] key)
    {
        return new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);
    }

    public static byte[] CreateKeyByteArrayFromJwtSecret(string jwtSecret)
    {
        return Encoding.ASCII.GetBytes(FormatSecretKey(jwtSecret));
    }

    public static string FormatSecretKey(string key)
    {
        int startIndex = string.IsNullOrEmpty(key) ? 0 : key.Length;

        for (int i = startIndex; i < 32; i++)
            key += '0';

        if (key.Length > 32)
            key = key.Substring(0, 32);

        return key;
    }
}
