using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Web.API.IntegrationTests.Common;

namespace Web.API.IntegrationTests.SignalR;

public static class Utils
{
    public static HubConnection CreateHubConnection<T>(WebApplicationFactory<T> factory, string userId) where T : class
    {
        return new HubConnectionBuilder()
            .WithUrl(Constants.DefaultHubNotificationUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();

                options.Transports = HttpTransportType.LongPolling;

                options.Headers[Constants.UserIdRequestHeaderName] = userId;
                options.Headers[Constants.UserNameRequestHeaderName] = Constants.DefaultTestUserName;
                options.Headers[Constants.UserRoleRequestHeaderName] = Constants.DefaultTestUserRole;
            })
            .Build();
    }
}
