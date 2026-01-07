using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace Web.API.IntegrationTests.SignalR;

public static class Utils
{
    public static HubConnection CreateHubConnection<T>(WebApplicationFactory<T> factory, string userId) where T : class
    {
        return new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notification", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();

                options.Transports = HttpTransportType.LongPolling;

                options.Headers["x-user-id"] = userId;
                options.Headers["x-test-user"] = "root";
                options.Headers["x-test-role"] = "Admin";
            })
            .Build();
    }
}
