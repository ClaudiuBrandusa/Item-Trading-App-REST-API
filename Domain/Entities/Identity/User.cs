using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Identity;

public class User : IdentityUser
{
    public int Cash { get; private set; }

    public void UpdateCashAmount(int amount)
    {
        Cash = amount;
    }

    public static string GenerateId() => Guid.NewGuid().ToString();
}
