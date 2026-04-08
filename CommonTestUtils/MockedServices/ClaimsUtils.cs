using System.Security.Claims;
using Domain.Entities.Identity;

namespace CommonTestUtils.MockedServices;

public static class ClaimsUtils
{
    public static ClaimsPrincipal CreateDefaultUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("id", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Admin")
        ], authenticationType: "Test"));
    }

    public static ClaimsPrincipal CreateClaimsFromUser(User user)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("id", user.Id),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Role, "Admin")
        ], authenticationType: "Test"));
    }
}
