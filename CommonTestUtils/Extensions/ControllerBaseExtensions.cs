using CommonTestUtils.MockedServices;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CommonTestUtils.Extensions;

public static class ControllerBaseExtensions
{
    public static void SetSenderUser(this ControllerBase controller, User user)
    {
        var httpContext = new DefaultHttpContext
        {
            User = ClaimsUtils.CreateClaimsFromUser(user)
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }
}
