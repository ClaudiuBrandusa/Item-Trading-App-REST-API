using Application.Services.ConnectedUsers;
using Infrastructure.Wrappers.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Web.API.IntegrationTests.Common.Factories;

namespace Web.API.IntegrationTests.SignalR;

public class NotificationHubTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public NotificationHubTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NotifyUserAsync_Test()
    {
        var userId = "user-123";

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var connection = CreateHubConnection(userId);

        connection.On<string>("notify", msg =>
        {
            tcs.TrySetResult(msg);
        });

        connection.On<dynamic>("notify", msg =>
        {
            tcs.TrySetResult(msg);
        });

        connection.On("notify", () =>
        {
            tcs.TrySetResult(null);
        });

        await connection.StartAsync();

        var service = _factory.Services.GetRequiredService<IConnectedUsersRepository>();

        var connectionsForUserId = service.ListConnectionIdsForUserId(userId);
        var userIds = service.ListUserIds();

        var received = await tcs.Task;
        Assert.NotEmpty(received);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task NotifyUser_NotifyThroughHubContext_ShouldReceiveTheCorrectNotification()
    {
        var userId = "user-123";
        var expectedNotification = "notification content";

        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        var connection = CreateHubConnection(userId);

        connection.On<string>("notify", msg =>
        {
            tcs.TrySetResult(msg);
        });

        await connection.StartAsync();

        var service = _factory.Services.GetRequiredService<IHubContextWrapper>();

        await service.NotifyUserAsync(userId, expectedNotification);

        var received = await tcs.Task;

        Assert.Equal(expectedNotification, received);

        await connection.DisposeAsync();
    }

    private HubConnection CreateHubConnection(string userId)
    {
        return new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notification", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();

                options.Transports = HttpTransportType.LongPolling;

                options.Headers["x-user-id"] = userId;
                options.Headers["x-test-user"] = "root";
                options.Headers["x-test-role"] = "Admin";
            })
            .Build();
    }
}