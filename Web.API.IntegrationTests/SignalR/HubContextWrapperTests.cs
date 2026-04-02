using Infrastructure.Wrappers.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Web.API.IntegrationTests.Common.Factories;

namespace Web.API.IntegrationTests.SignalR;

public class HubContextWrapperTests : IClassFixture<DbOnlyTestAppFactory>
{
    private readonly DbOnlyTestAppFactory _factory;

    public HubContextWrapperTests(DbOnlyTestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NotifyUser_NotifyThroughHubContext_ShouldReceiveTheCorrectNotification()
    {
        var userId = "user-123";
        var expectedNotification = "notification content";

        var tcs = new TaskCompletionSource<string>();

        var connection = Utils.CreateHubConnection(_factory, userId);

        connection.On<string>("notify", msg =>
        {
            tcs.TrySetResult(msg);
        });

        await connection.StartAsync(CancellationToken.None);

        var service = _factory.Services.GetRequiredService<IHubContextWrapper>();

        await service.NotifyUserAsync(userId, expectedNotification);

        var received = await tcs.Task;
        var str = JsonSerializer.Deserialize<string>(received);
        Assert.Equal(expectedNotification, str);

        await connection.DisposeAsync();
    }
}
