using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Item_Trading_App_REST_API.Controllers
{
    public class BaseController : Controller
    {
        protected string UserId
        {
            get => User.Claims.First(c => Equals(c.Type, "id"))?.Value;
        }
    }
}
