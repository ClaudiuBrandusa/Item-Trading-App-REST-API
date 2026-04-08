using System.Security.Claims;
using CommonTestUtils.MockedServices;
using Domain.Entities.Identity;
using Infrastructure.Data;

namespace CommonTestUtils.Extensions;

public static class DatabaseContextExtensions
{
    public static User? GetUserByName(this DatabaseContext dbContext, string username)
    {
        var identityUser = dbContext.Users.FirstOrDefault(x => x.UserName == username);
        
        return identityUser as User;
    }

    public static (User, ClaimsPrincipal) GetUserWithClaimsByName(this DatabaseContext dbContext, string username)
    {
        var user = dbContext.GetUserByName(username)!;
        var userClaims = ClaimsUtils.CreateClaimsFromUser(user)!;

        return (user, userClaims);
    }
}
