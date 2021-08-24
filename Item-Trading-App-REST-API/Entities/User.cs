using Microsoft.AspNetCore.Identity;

namespace Item_Trading_App_REST_API.Entities
{
    public class User : IdentityUser
    {
        public int Cash { get; set; }
    }
}
