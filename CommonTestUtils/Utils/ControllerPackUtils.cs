using System.Security.Claims;
using CommonTestUtils.MockedServices;
using CommonTestUtils.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CommonTestUtils.Utils;

public static class ControllerPackUtils
{
    public static ControllerPack<T, E> CreateControllerPackWithUser<T, E>(WebApplicationFactory<E> factory, ClaimsPrincipal user)
        where T : Controller
        where E : class
    {
        var controllerPack = new ControllerPack<T, E>(factory);

        controllerPack.SetUser(user);

        return controllerPack;
    }

    public static ControllerPack<T, E> CreateControllerPackWithDefaultUser<T, E>(WebApplicationFactory<E> factory)
        where T : Controller
        where E : class
    {
        var controllerPack = new ControllerPack<T, E>(factory);

        var user = ClaimsUtils.CreateDefaultUser();

        controllerPack.SetUser(user);

        return controllerPack;
    }
}
