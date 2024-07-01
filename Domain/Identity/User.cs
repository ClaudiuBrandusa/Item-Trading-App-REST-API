using Microsoft.AspNetCore.Identity;

namespace Domain.Identity;

public class User : IdentityUser
{
    public int Cash { get; set; }
}
