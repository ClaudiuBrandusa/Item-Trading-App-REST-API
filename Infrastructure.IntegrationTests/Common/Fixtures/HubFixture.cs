using Application.Services.Cache;
using Application.Services.ConnectedUsers;
using Application.Services.Notification;
using Infrastructure.IntegrationTests.Common.Mocks;
using Infrastructure.IntegrationTests.Hub;
using Infrastructure.Services.ConnectedUsers;
using Infrastructure.Services.Notification;
using Infrastructure.Wrappers.Hubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Infrastructure.IntegrationTests.Common.Fixtures;

public class HubFixture : IAsyncLifetime
{
    private readonly string hubEndpoint = "/hubs/notification";

    public TestServer Server { get; private set; } = null!;
    public IHost Host { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        Host = await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer()
                   .ConfigureServices(services =>
                   {
                       services.AddRouting();
                       services.AddSignalR(o => o.EnableDetailedErrors = true);

                       services.AddAuthentication(options =>
                       {
                           options.DefaultAuthenticateScheme = HeaderAuthHandler.Scheme;
                           options.DefaultChallengeScheme = HeaderAuthHandler.Scheme;
                       })
                        .AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>(
                            HeaderAuthHandler.Scheme, _ => { });

                       services.AddAuthorization();

                       services.AddSingleton<IHubContextWrapper, HubContextWrapperImplementation>();

                       var cacheService = new Mock<ICacheService>().Object;

                       services.AddSingleton((_) => cacheService);
                       services.AddSingleton<IConnectedUsersRepository, ConnectedUsersRepository>();
                       services.AddTransient<IClientNotificationService, ClientNotificationService>();
                   })
                   .Configure(app =>
                   {
                       app.UseRouting();
                       app.UseAuthentication();
                       app.UseAuthorization();
                       app.UseEndpoints(endpoints =>
                       {
                           endpoints.MapHub<NotificationHubImpl>(hubEndpoint);
                       });
                   });
            })
            .StartAsync();

        Server = Host.GetTestServer();
    }

    public async ValueTask DisposeAsync()
    {
        await Host.StopAsync();
        Server.Dispose();
    }
}
