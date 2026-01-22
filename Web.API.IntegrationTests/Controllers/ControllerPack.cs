using System.Security.Claims;
using Item_Trading_App_REST_API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

public class ControllerPack<Controller> : IDisposable
     where Controller : BaseController
    {
        public Controller ControllerInstance { get; init; }

        public IServiceScope ServiceScope { get; init; }

        public ControllerPack(WebApplicationFactory<Program> factory)
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

        private Controller CreateController()
        {
            return ActivatorUtilities.CreateInstance<Controller>(ServiceScope.ServiceProvider);
        }
    }