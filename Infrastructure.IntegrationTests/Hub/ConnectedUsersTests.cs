using Application.Services.ConnectedUsers;
using Infrastructure.IntegrationTests.Common.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.IntegrationTests.Hub;

public class ConnectedUsersTests : IClassFixture<HubFixture>
{
    private readonly HubFixture _hubFixture;
    private readonly IConnectedUsersRepository _connectedUsersRepository;

    public ConnectedUsersTests(HubFixture fixture)
    {
        _hubFixture = fixture;

        _connectedUsersRepository = _hubFixture.Server.Services.GetRequiredService<IConnectedUsersRepository>();
    }

    [Fact]
    public async Task AddConnectionIdToUser_AddUserThenCheck_ShouldFindUserIdAndUserConnection()
    {
        var expectedConnectionId = Guid.NewGuid().ToString();
        var expectedUserId = Guid.NewGuid().ToString();
        var expectedUserName = "User name";

        var connectedUsersRepository = _hubFixture.Server.Services.GetRequiredService<IConnectedUsersRepository>();

        var result = await connectedUsersRepository.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);

        var userIds = connectedUsersRepository.ListUserIds();
        var userConnections = connectedUsersRepository.ListConnectionIdsForUserId(expectedUserId);

        Assert.Contains(expectedUserId, userIds);
        Assert.Contains(expectedUserId, userIds);
    }

    [Fact]
    public async Task RemoveConnectionIdFromUser_AddUserThenRemoveAndCheck_ShouldNoLongerFindUserIdAndUserConnection()
    {
        var expectedConnectionId = Guid.NewGuid().ToString();
        var expectedUserId = Guid.NewGuid().ToString();
        var expectedUserName = "User name";

        var connectedUsersRepository = _hubFixture.Server.Services.GetRequiredService<IConnectedUsersRepository>();

        await connectedUsersRepository.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        await connectedUsersRepository.RemoveConnectionIdFromUser(expectedConnectionId, expectedUserId);

        var userIds = connectedUsersRepository.ListUserIds();
        var userConnections = connectedUsersRepository.ListConnectionIdsForUserId(expectedUserId);

        Assert.Empty(userIds);
        Assert.Empty(userConnections);
    }

    [Fact]
    public async Task NotifyUser_AddUserThenNotify_ShouldReceiveNotification()
    {
        var hubEndpoint = "hubs/notification";

        var server = _hubFixture.Server;

        var expectedUserId = Guid.NewGuid().ToString();
        var expectedUserName = "User name";
        var notification = new NotificationMock
        {
            Data = "Some data"
        };

        var connectedUsersRepository = _hubFixture.Server.Services.GetRequiredService<IConnectedUsersRepository>();

        var notificationTaskSource = new TaskCompletionSource<NotificationMock>();

        var connection = CreateHubConnection(expectedUserId, expectedUserName, hubEndpoint, server);

        connection.Closed += (exception) =>
        {
            if (exception is not null)
                notificationTaskSource.TrySetException(exception);

            return Task.CompletedTask;
        };

        await connection.StartAsync();
        
        Assert.Equal(HubConnectionState.Connected, connection.State);
        var expectedConnectionId = connection.ConnectionId;
        Assert.NotNull(expectedConnectionId);

        connection.On<string>("notify", (string notificationJson) =>
        {
            var notification = JsonSerializer.Deserialize<NotificationMock>(notificationJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            notificationTaskSource.TrySetResult(notification);
        });

        await connectedUsersRepository.NotifyUserAsync(expectedUserId, notification);

        var userIds = connectedUsersRepository.ListUserIds();
        var userConnections = connectedUsersRepository.ListConnectionIdsForUserId(expectedUserId);

        Assert.Contains(expectedUserId, userIds);
        Assert.Single(userIds);
        Assert.Contains(expectedConnectionId, userConnections);
        Assert.Single(userConnections);

        var receivedNotification = await notificationTaskSource.Task;
        Assert.Equivalent(notification, receivedNotification);

        await connection.DisposeAsync();
    }

    public static HubConnection CreateHubConnection(string userId, string userName, string hubEndpoint, TestServer server)
    {
        return new HubConnectionBuilder()
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Trace);
            })
            .WithUrl($"http://localhost/{hubEndpoint}", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.SkipNegotiation = false;
                options.Transports = HttpTransportType.LongPolling;
                
                options.Headers["x-user-id"] = userId;
                options.Headers["x-test-user"] = userName;
            })
            .Build();
    }

    public class NotificationMock
    {
        public string Data { get; set; }
    }
}
