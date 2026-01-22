using System.Security.Claims;

namespace Web.API.IntegrationTests.Controllers;

public static class Utils
{
    public static ClaimsPrincipal CreateDefaultUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("id", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Admin")
        }, authenticationType: "Test"));
    }
}
