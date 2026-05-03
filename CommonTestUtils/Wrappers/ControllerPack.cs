using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CommonTestUtils.Wrappers;

public class ControllerPack<ControllerType, TEntryPoint> : IDisposable
     where ControllerType : Controller
     where TEntryPoint : class
{
    public ControllerType ControllerInstance { get; init; }

    public IServiceScope ServiceScope { get; init; }

    public ControllerPack(WebApplicationFactory<TEntryPoint> factory)
    {
        ServiceScope = factory.Services.CreateScope();
        ControllerInstance = CreateController();
    }

    public void SetUser(ClaimsPrincipal user)
    {
        ControllerInstance.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = ServiceScope.ServiceProvider,
                User = user
            }
        };
    }

    public void Dispose()
    {
        ControllerInstance.Dispose();
        ServiceScope.Dispose();
    }

    private ControllerType CreateController()
    {
        return ActivatorUtilities.CreateInstance<ControllerType>(ServiceScope.ServiceProvider);
    }
}
