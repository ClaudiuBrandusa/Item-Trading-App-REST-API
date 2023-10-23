using System.Linq;
using System.Security.Claims;

namespace Item_Trading_App_REST_API.Extensions;

public static class UserExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal) 
    {
        return principal.Claims.First(c => Equals(c.Type, "id"))?.Value;
    }
}
