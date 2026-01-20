using Application.Services.ConnectedUsers;
using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_Contracts.Notifications.Content;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Web.API.IntegrationTests.Common.Factories;

namespace Web.API.IntegrationTests.SignalR;

public class NotificationHubTests : IClassFixture<DbOnlyTestAppFactory>
{
    private readonly DbOnlyTestAppFactory _factory;

    public NotificationHubTests(DbOnlyTestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NotifyUserAsync_Test()
    {
        var userId = "user-123";

        var tcs = new TaskCompletionSource<string>();

        var connection = Utils.CreateHubConnection(_factory, userId);

        connection.On<string>("notify", msg =>
        {
            tcs.TrySetResult(msg);
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
    public async Task NotifyUserAsync_ExtractNotificationFromHub_ShouldContainNotification()
    {
        var userId = "user-123";

        var tcs = new TaskCompletionSource<string>();

        var connection = Utils.CreateHubConnection(_factory, userId);

        connection.On<string>("notify", msg =>
        {
            tcs.TrySetResult(msg);
        });

        await connection.StartAsync();

        var received = await tcs.Task;
        Assert.NotEmpty(received);
        var notification = JsonSerializer.Deserialize<Notification<MessageContent>>(received, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.NotNull(notification);
        Assert.NotEmpty(notification.Type);
        var notificationContent = notification.Content;
        Assert.NotNull(notificationContent);
        Assert.NotNull(notificationContent.Content);
        Assert.NotEqual(default, notificationContent.CreatedDateTime);

        await connection.DisposeAsync();
    }
}