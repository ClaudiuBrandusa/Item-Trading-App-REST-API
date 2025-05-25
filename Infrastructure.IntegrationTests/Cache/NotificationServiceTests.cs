using Application.Constants;
using Application.Services.Cache;
using Application.Services.ConnectedUsers;
using Application.Services.Notification;
using Infrastructure.IntegrationTests.Common;
using Infrastructure.Services.ConnectedUsers;
using Infrastructure.Services.Notification;
using Item_Trading_App_REST_API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Infrastructure.IntegrationTests.Cache;

public class NotificationServiceTests : IClassFixture<CachingFixture>
{
    private readonly IClientNotificationService _sut;
    private Dictionary<string, List<string>> groupManagerConnections;

    public NotificationServiceTests(CachingFixture fixture)
    {
        var serviceProvider = fixture.ServiceProvider;

        var cacheService = serviceProvider.GetRequiredService<ICacheService>();

        groupManagerConnections = new();

        var hubContextMock = GetHubContextMock(groupManagerConnections);

        var connectedUsersRepository = new ConnectedUsersRepository(cacheService, hubContextMock.Object);

        var hubClientsWrapper = new HubClientsWrapper(hubContextMock.Object);

        _sut = new ClientNotificationService(connectedUsersRepository, hubClientsWrapper);
    }

    [Fact(DisplayName = "Send a message notification")]
    public async Task SendMessageNotification()
    {
        // Arrange

        string content = "Message";
        bool notificationReceived = false;

        var notifyStrategyMock = GetNotifyUserStrategyMock(() => notificationReceived = true);

        // Act

        await _sut.SendMessageNotificationAsync(notifyStrategyMock.Object, content, DateTime.Now);
        
        // Assert

        Assert.True(notificationReceived);
    }

    [Fact(DisplayName = "Send a created notification")]
    public async Task SendCreatedNotification()
    {
        // Arrange

        bool notificationReceived = false;

        var notifyStrategyMock = GetNotifyUserStrategyMock(() => notificationReceived = true);
        
        // Act

        await _sut.SendCreatedNotificationAsync(notifyStrategyMock.Object, NotificationCategoryTypes.Item, "id");

        // Assert

        Assert.True(notificationReceived);
    }

    [Fact(DisplayName = "Send an updated notification")]
    public async Task SendUpdatedNotification()
    {
        // Arrange

        bool notificationReceived = false;

        var notifyStrategyMock = GetNotifyUserStrategyMock(() => notificationReceived = true);

        // Act

        await _sut.SendUpdatedNotificationAsync(notifyStrategyMock.Object, NotificationCategoryTypes.Item, "id");

        // Assert

        Assert.True(notificationReceived);
    }

    [Fact(DisplayName = "Send a deleted notification")]
    public async Task SendDeletedNotification()
    {
        // Arrange

        bool notificationReceived = false;

        var notifyStrategyMock = GetNotifyUserStrategyMock(() => notificationReceived = true);

        // Act

        await _sut.SendDeletedNotificationAsync(notifyStrategyMock.Object, NotificationCategoryTypes.Item, "id");

        // Assert

        Assert.True(notificationReceived);
    }

    #region Utils

    private Mock<IHubContext<NotificationHubBase>> GetHubContextMock(Dictionary<string, List<string>> groupManagerConnections)
    {
        var hubContextMock = new Mock<IHubContext<NotificationHubBase>>();

        var groupManagerMock = new Mock<IGroupManager>();

        groupManagerMock.Setup(x => x.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string connectionId, string groupName, CancellationToken cancellationToken) =>
            {
                List<string> connections;

                if (!groupManagerConnections.TryGetValue(groupName, out connections))
                {
                    connections = new List<string>();
                }

                connections.Add(connectionId);

                groupManagerConnections.Add(groupName, connections);
            });

        groupManagerMock.Setup(x => x.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string connectionId, string groupName, CancellationToken cancellationToken) =>
            {
                if (groupManagerConnections.TryGetValue(groupName, out List<string> connections))
                {
                    if (connections.Count == 1)
                    {
                        groupManagerConnections.Remove(groupName);

                        return;
                    }

                    int index = connections.IndexOf(connectionId);

                    if (index == -1)
                        return;

                    connections.RemoveAt(index);
                }
            });

        hubContextMock.SetupGet(x => x.Groups).Returns(groupManagerMock.Object);

        return hubContextMock;
    }

    private Mock<INotifyUserStrategy> GetNotifyUserStrategyMock(Action action)
    {
        var notifyStrategyMock = new Mock<INotifyUserStrategy>();

        notifyStrategyMock.Setup(x => x.Notify(It.IsAny<IHubClientsWrapper>(), It.IsAny<IConnectedUsersRepository>(), It.IsAny<object>()))
            .Callback((IHubClientsWrapper hubClientsWrapper, IConnectedUsersRepository connectedUsersRepository, object notification) =>
            {
                action.Invoke();
            });

        return notifyStrategyMock;
    }

    #endregion Utils
}
