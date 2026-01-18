using Application.Services.ConnectedUsers;
using Infrastructure.IntegrationTests.Common.Fixtures;
using Infrastructure.IntegrationTests.Hub.Common;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Infrastructure.IntegrationTests.Hub;

public class ConnectedUsersTests : IClassFixture<HubFixture>
{
    private readonly HubFixture _hubFixture;
    private readonly IConnectedUsersRepository _connectedUsersRepository;
    private const string _hubEndpoint = "hubs/notification";
    private const int DefaultMaxDegreeOfParallelism = 10;

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

        Assert.DoesNotContain(expectedUserId, userIds);
        Assert.DoesNotContain(expectedConnectionId, userConnections);
    }

    [Fact]
    public async Task NotifyUser_AddUserThenNotify_ShouldReceiveNotification()
    {
        var server = _hubFixture.Server;

        var expectedUserId = Guid.NewGuid().ToString();
        var expectedUserName = "User name";
        var notification = new NotificationMock
        {
            Data = "Some data"
        };

        var connectedUsersRepository = _hubFixture.Server.Services.GetRequiredService<IConnectedUsersRepository>();

        var notificationTaskSource = new TaskCompletionSource<NotificationMock>();

        var connection = ConnectedClient.CreateHubConnection(expectedUserId, expectedUserName, _hubEndpoint, server);

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

        var listener = connection.On<string>("notify", (string notificationJson) =>
        {
            var notification = JsonSerializer.Deserialize<NotificationMock>(notificationJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            notificationTaskSource.TrySetResult(notification);
        });

        await connectedUsersRepository.NotifyUserAsync(expectedUserId, notification);

        var receivedNotification = await notificationTaskSource.Task;
        listener.Dispose();
        Assert.Equivalent(notification, receivedNotification);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task NotifyUsers_AddUsersThenNotify_AllUsersShouldReceiveNotification()
    {
        var server = _hubFixture.Server;
        var expectedUsersAmount = 5;

        var users = new (string userId, string userName)[expectedUsersAmount];

        for (int i = 0; i < expectedUsersAmount; i++)
        {
            var userId = Guid.NewGuid().ToString();
            var userName = $"User {i}";

            users[i] = (userId, userName);
        }

        var notification = new NotificationMock
        {
            Data = "Some data"
        };

        var connectedUsersRepository = _hubFixture.Server.Services.GetRequiredService<IConnectedUsersRepository>();

        var connectedClients = new ConnectedClient[expectedUsersAmount];

        var notificationListeners = new IDisposable[expectedUsersAmount];

        var taskCompletionSources = new TaskCompletionSource<NotificationMock>[expectedUsersAmount];

        var connectionsSetupTasks = new Task[expectedUsersAmount];

        await Parallel.ForEachAsync(Enumerable.Range(0, users.Length),
            new ParallelOptions { MaxDegreeOfParallelism = DefaultMaxDegreeOfParallelism },
            async (index, ct) =>
            {
                var user = users[index];

                var connectedClient = new ConnectedClient(user.userId, user.userName, _hubEndpoint, server);

                await connectedClient.Connect();

                connectedClients[index] = connectedClient;

                var tcs = new TaskCompletionSource<NotificationMock>();

                taskCompletionSources[index] = tcs;

                notificationListeners[index] = connectedClient.Listen<string>("notify", (string notificationJson) =>
                {
                    if (tcs.Task.IsCompleted)
                        return;

                    var notification = JsonSerializer.Deserialize<NotificationMock>(notificationJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (string.IsNullOrEmpty(notification.Data) && (notificationJson.Contains("Welcome!") || notificationJson.Contains("has connected!")))
                        return; // then we received the greetings notification

                    tcs.TrySetResult(notification);
                });
            }
        );

        await Task.WhenAny(
            Task.WhenAll(connectedClients.Select(x => x.Connected)),
            Task.Delay(5 * 1000)
        );

        var notificationTasks = taskCompletionSources.Select(x => x.Task).ToArray();

        await connectedUsersRepository.NotifyUsersAsync(users.Select(x => x.userId).ToArray(), notification);

        await Task.WhenAll(notificationTasks);

        var receivedNotifications = new NotificationMock[expectedUsersAmount];

        for (int i = 0; i < notificationTasks.Length; i++)
        {
            var task = notificationTasks[i];
            receivedNotifications[i] = await task;
        }

        Assert.All(receivedNotifications, receivedNotification =>
        {
            Assert.Equivalent(notification, receivedNotification);
        });

        foreach (var connectedClient in connectedClients)
        {
            connectedClient.Dispose();
        }

        foreach (var listener in notificationListeners)
        {
            listener.Dispose();
        }
    }

    [Fact]
    public async Task ConnectUsers_ConnectManyUsersInParallel_ShouldWorkJustFine()
    {
        var server = _hubFixture.Server;
        var expectedUsersAmount = 50;

        var users = new (string userId, string userName)[expectedUsersAmount];

        for (int i = 0; i < expectedUsersAmount; i++)
        {
            var userId = Guid.NewGuid().ToString();
            var userName = $"User {i}";

            users[i] = (userId, userName);
        }

        var connectedUsersRepository = _hubFixture.Server.Services.GetRequiredService<IConnectedUsersRepository>();

        var connectedClients = new ConnectedClient[expectedUsersAmount];

        var connectionsSetupTasks = new Task[expectedUsersAmount];

        await Parallel.ForEachAsync(Enumerable.Range(0, users.Length),
            new ParallelOptions { MaxDegreeOfParallelism = DefaultMaxDegreeOfParallelism },
            async (index, ct) =>
            {
                var user = users[index];

                var connectedClient = new ConnectedClient(user.userId, user.userName, _hubEndpoint, server);

                connectedClients[index] = connectedClient;

                await connectedClient.Connect();
            }
        );

        await Task.WhenAny(
            Task.WhenAll(connectedClients.Select(x => x.Connected)),
            Task.Delay(5 * 1000)
        );

        var userIds = connectedUsersRepository.ListUserIds();

        Assert.NotNull(userIds);
        Assert.Equal(expectedUsersAmount, userIds.Length);
        Assert.All(users, user =>
        {
            Assert.Contains(user.userId, userIds);

            var connectionIds = connectedUsersRepository.ListConnectionIdsForUserId(user.userId);

            Assert.NotNull(connectionIds);
            Assert.Single(connectionIds);
            Assert.NotEmpty(connectionIds[0]);
        });

        foreach (var connectedClient in connectedClients)
        {
            connectedClient.Dispose();
        }
    }

    [Fact]
    public async Task ConnectUsers_ConnectManyConnectionsForSameUsersInParallel_ShouldWorkJustFine()
    {
        var server = _hubFixture.Server;
        var expectedUsersAmount = 10;
        var expectedAmountOfConnectionsForUserId = 5;

        var users = new (string userId, string userName)[expectedUsersAmount];

        for (int i = 0; i < expectedUsersAmount; i++)
        {
            var userId = Guid.NewGuid().ToString();
            var userName = $"User {i}";

            users[i] = (userId, userName);
        }

        var connectedUsersRepository = _hubFixture.Server.Services.GetRequiredService<IConnectedUsersRepository>();

        var connectedClients = new ConnectedClient[expectedUsersAmount, expectedAmountOfConnectionsForUserId];

        var connectionsSetupTasks = new Task[expectedUsersAmount];

        await Parallel.ForEachAsync(Enumerable.Range(0, users.Length),
            new ParallelOptions { MaxDegreeOfParallelism = DefaultMaxDegreeOfParallelism },
            async (index, ct) =>
            {
                await Parallel.ForEachAsync(Enumerable.Range(0, expectedAmountOfConnectionsForUserId),
                    new ParallelOptions { MaxDegreeOfParallelism = 1 },
                    async (connectionIndex, ct0) =>
                    {
                        var user = users[index];

                        var connectedClient = new ConnectedClient(user.userId, user.userName, _hubEndpoint, server);

                        connectedClients[index, connectionIndex] = connectedClient;

                        await connectedClient.Connect();
                    }
                );
            }
        );

        await Task.WhenAny(
            Task.WhenAll(connectedClients.Cast<ConnectedClient>().Select(x => x.Connected).Where(x => !x.IsCompleted)),
            Task.Delay(5 * 1000)
        );

        var userIds = connectedUsersRepository.ListUserIds();

        Assert.NotNull(userIds);
        Assert.True(userIds.Length >= expectedUsersAmount);
        Assert.All(users, user =>
        {
            Assert.Contains(user.userId, userIds);

            var connectionIds = connectedUsersRepository.ListConnectionIdsForUserId(user.userId);

            Assert.NotNull(connectionIds);
            Assert.Equal(expectedAmountOfConnectionsForUserId, connectionIds.Length);
            Assert.All(connectionIds, Assert.NotEmpty);
        });

        foreach (var connectedClient in connectedClients)
        {
            connectedClient.Dispose();
        }
    }

    [Fact]
    public async Task Notify_ConnectManyUsersAndSendNotificationsInParallel_ShouldWorkJustFine()
    {
        var expectedDataContent = "Test";

        var server = _hubFixture.Server;
        var expectedUsersAmount = 10;
        var expectedNotification = new NotificationMock
        {
            Data = expectedDataContent
        };

        var users = new (string userId, string userName)[expectedUsersAmount];

        for (int i = 0; i < expectedUsersAmount; i++)
        {
            var userId = Guid.NewGuid().ToString();
            var userName = $"User {i}";

            users[i] = (userId, userName);
        }

        var connectedUsersRepository = _hubFixture.Server.Services.GetRequiredService<IConnectedUsersRepository>();

        var connectedClients = new ConnectedClient[expectedUsersAmount];

        var connectionsSetupTasks = new Task[expectedUsersAmount];

        var receivedBagKey = "received";

        await Parallel.ForEachAsync(Enumerable.Range(0, users.Length),
            new ParallelOptions { MaxDegreeOfParallelism = DefaultMaxDegreeOfParallelism },
            async (index, ct) =>
            {
                var user = users[index];

                var connectedClient = new ConnectedClient(user.userId, user.userName, _hubEndpoint, server);

                connectedClients[index] = connectedClient;

                await connectedClient.Connect();

                connectedClient.Listen<string>("notify", (notificationJson) =>
                {
                    var notification = JsonSerializer.Deserialize<NotificationMock>(notificationJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (string.IsNullOrEmpty(notification.Data) && (notificationJson.Contains("Welcome!") || notificationJson.Contains("has connected!")))
                        return;

                    connectedClient.ReceivedBag.Add(receivedBagKey, notification);
                });

                await connectedUsersRepository.NotifyUserAsync(user.userId, expectedNotification);
            });

        await Task.WhenAny(
            Task.WhenAll(connectedClients.Select(x => x.Connected)),
            Task.Delay(5 * 1000)
        );

        var userIds = connectedUsersRepository.ListUserIds();

        Assert.NotNull(userIds);
        Assert.True(userIds.Length >= expectedUsersAmount);
        Assert.All(users, user =>
        {
            Assert.Contains(user.userId, userIds);

            var connectionIds = connectedUsersRepository.ListConnectionIdsForUserId(user.userId);

            Assert.NotNull(connectionIds);
            Assert.Single(connectionIds);
            Assert.NotEmpty(connectionIds[0]);

            var connectedClient = connectedClients.FirstOrDefault(c => c.UserId == user.userId);

            Assert.NotNull(connectedClient);
            Assert.Contains(receivedBagKey, connectedClient.ReceivedBag.Keys);

            var notificationObject = connectedClient.ReceivedBag[receivedBagKey];

            Assert.NotNull(notificationObject);

            var notification = notificationObject as NotificationMock;

            Assert.NotNull(notification);
            Assert.Equal(expectedDataContent, notification.Data);
        });

        foreach (var connectedClient in connectedClients)
        {
            connectedClient.Dispose();
        }
    }

    public class NotificationMock
    {
        public string Data { get; set; }
    }
}
