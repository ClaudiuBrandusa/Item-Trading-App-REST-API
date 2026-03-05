using System.Security.Claims;
using Domain.Entities.Identity;
using Infrastructure.Data;
using static Web.API.IntegrationTests.Controllers.Common.Utils;

namespace Web.API.IntegrationTests.Controllers.Common;

public static class Extensions
{
    public static User? GetUserByName(this DatabaseContext dbContext, string username)
    {
        var identityUser = dbContext.Users.FirstOrDefault(x => x.UserName == username);
        
        return identityUser as User;
    }

    public static (User, ClaimsPrincipal) GetUserWithClaimsByName(this DatabaseContext dbContext, string username)
    {
        var user = dbContext.GetUserByName(username)!;
        var userClaims = CreateClaimsFromUser(user)!;

        return (user, userClaims);
    }
}