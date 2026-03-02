using Domain.Entities.Identity;
using Infrastructure.Data;

namespace Web.API.IntegrationTests.Controllers.Common;

public static class Extensions
{
    public static User? GetUserByName(this DatabaseContext dbContext, string username)
    {
        var identityUser = dbContext.Users.FirstOrDefault(x => x.UserName == username);
        
        return identityUser as User;
    }
}