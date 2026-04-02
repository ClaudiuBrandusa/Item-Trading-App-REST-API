using Domain.Primitives;

namespace Domain.Entities.Identity;

public class RefreshToken : Entity
{
    public string Token { get; private set; }

    public string JwtId { get; private set; }

    public DateTime CreationDate { get; private set; }

    public DateTime ExpiryDate { get; private set; }

    public bool Used { get; private set; }

    public bool Invalidated { get; private set; }

    public string UserId { get; private set; }

    public virtual User? User { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private RefreshToken() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public RefreshToken(string jwtId, string userId, TimeSpan refreshTokenLifetime)
    {
        Token = GenerateToken();
        JwtId = jwtId;
        UserId = userId;
        CreationDate = DateTime.UtcNow;
        ExpiryDate = GenerateExpiryDate(refreshTokenLifetime);
    }

    protected override bool Compare(object obj)
    {
        if (obj is not RefreshToken entity) return false;

        return entity.Token == Token &&
               entity.JwtId == JwtId &&
               entity.UserId == UserId;
    }

    protected override object GetId() => Token;

    public static string GenerateToken() => Guid.NewGuid().ToString();

    public static DateTime GenerateExpiryDate(TimeSpan refreshTokenLifetime) => DateTime.UtcNow.Add(refreshTokenLifetime);
}
