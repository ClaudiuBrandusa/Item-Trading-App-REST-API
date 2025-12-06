using Application.Services.Cache;
using Infrastructure.Services.ConnectedUsers;
using Infrastructure_IntegrationTests.Utils;
using Item_Trading_App_REST_API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Infrastructure_UnitTests.RepositoryTests;

public class ConnectedUsersRepositoryTests
{
    [Fact]
    public async Task AddConnectionIdToUser_AddsANewConnection_ShouldCallTheAddGroupMethod()
    {
        var expectedConnectionId = "connectionId";
        var expectedUserId = "userId";
        var expectedUserName = "userName";

        (
            var sut,
            var cacheServiceMock,
            var hubContextMock,
            var groupManagerMock,
            _
        ) = CreateRepositoryAndGetDependencyMocks();

        var result = await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        var connectionsForUserId = sut.ListConnectionIdsForUserId(expectedUserId);

        groupManagerMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);

        Assert.NotNull(connectionsForUserId);
        Assert.Contains(expectedConnectionId, connectionsForUserId);
    }

    [Fact]
    public async Task RemoveConnectionIdFromUser_AddsANewConnectionThenRemovesIt_ShouldCallTheAddGroupMethod()
    {
        var expectedConnectionId = "connectionId";
        var expectedUserId = "userId";
        var expectedUserName = "userName";

        (
            var sut,
            var cacheServiceMock,
            var hubContextMock,
            var groupManagerMock,
            _
        ) = CreateRepositoryAndGetDependencyMocks();

        var addConnectionResult = await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        await sut.RemoveConnectionIdFromUser(expectedConnectionId, expectedUserId);
        var connectionsForUserId = sut.ListConnectionIdsForUserId(expectedUserId);

        groupManagerMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);

        Assert.NotNull(connectionsForUserId);
        Assert.Empty(connectionsForUserId);
    }

    [Fact]
    public async Task NotifyUser_AddsANewConnectionNotifiesTheUser_ShouldNotifyTheUser()
    {
        var expectedConnectionId = "connectionId";
        var expectedUserId = "userId";
        var expectedUserName = "userName";

        (
            var sut,
            var cacheServiceMock,
            var hubContextMock,
            var groupManagerMock,
            var hubClientsMock
        ) = CreateRepositoryAndGetDependencyMocks();

        var notificationMock = new { };

        var clientProxyMock = new Mock<IClientProxy>();
        var clientProxy = clientProxyMock.Object;

        hubClientsMock.Setup(x => x.Group(expectedUserId))
            .Returns(clientProxy);

        var addConnectionResult = await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        await sut.NotifyUserAsync(expectedUserId, notificationMock);

        groupManagerMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);
        hubClientsMock.Verify(x => x.Group(expectedUserId), Times.Once);
    }

    [Fact]
    public async Task NotifyUsers_AddsANewConnectionNotifiesAllUsers_ShouldNotifyTheUser()
    {
        var expectedConnectionId = "connectionId";
        var expectedUserId = "userId";
        var expectedUserName = "userName";
        var expectedUserIds = new string[] { expectedUserId };

        (
            var sut,
            var cacheServiceMock,
            var hubContextMock,
            var groupManagerMock,
            var hubClientsMock
        ) = CreateRepositoryAndGetDependencyMocks();

        var notificationMock = new { };

        var clientProxyMock = new Mock<IClientProxy>();
        var clientProxy = clientProxyMock.Object;

        hubClientsMock.Setup(x => x.Groups(expectedUserIds))
            .Returns(clientProxy);

        var addConnectionResult = await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        await sut.NotifyUsersAsync(notificationMock);

        groupManagerMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);
        hubClientsMock.Verify(x => x.Groups(expectedUserIds), Times.Once);
    }

    [Fact]
    public async Task NotifyUsers_AddsANewConnectionNotifiesMultipleUsers_ShouldNotifyTheUser()
    {
        var expectedConnectionId = "connectionId";
        var expectedUserId = "userId";
        var expectedUserName = "userName";
        var expectedUserIds = new string[] { expectedUserId };

        (
            var sut,
            var cacheServiceMock,
            var hubContextMock,
            var groupManagerMock,
            var hubClientsMock
        ) = CreateRepositoryAndGetDependencyMocks();

        var notificationMock = new { };

        var clientProxyMock = new Mock<IClientProxy>();
        var clientProxy = clientProxyMock.Object;

        hubClientsMock.Setup(x => x.Groups(expectedUserIds))
            .Returns(clientProxy);

        var addConnectionResult = await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        await sut.NotifyUsersAsync(expectedUserIds, notificationMock);

        groupManagerMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);
        hubClientsMock.Verify(x => x.Groups(expectedUserIds), Times.Once);
    }

    [Fact]
    public async Task NotifyAllUsersExcept_AddsANewConnectionNotifiesTheUser_ShouldNotifyTheUser()
    {
        var expectedConnectionId = "connectionId";
        var expectedUserId = "userId";
        var exceptedUserId = "userId1";
        var expectedUserName = "userName";
        var expectedUserIds = new string[] { expectedUserId };

        (
            var sut,
            var cacheServiceMock,
            var hubContextMock,
            var groupManagerMock,
            var hubClientsMock
        ) = CreateRepositoryAndGetDependencyMocks();

        var notificationMock = new { };

        var clientProxyMock = new Mock<IClientProxy>();
        var clientProxy = clientProxyMock.Object;

        hubClientsMock.Setup(x => x.Groups(expectedUserIds))
            .Returns(clientProxy);

        await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        await sut.AddConnectionIdToUser(expectedConnectionId, exceptedUserId, expectedUserName);
        await sut.NotifyAllUsersExceptAsync(exceptedUserId, notificationMock);

        groupManagerMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);
        groupManagerMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, exceptedUserId, CancellationToken.None), Times.Once);
        hubClientsMock.Verify(x => x.Groups(expectedUserIds), Times.Once);
    }

    private Mock<IGroupManager> GetGroupManagerMock() => new Mock<IGroupManager>();

    private Mock<IHubClients> GetHubClientsMock() => new Mock<IHubClients>();

    private Mock<IHubContext<NotificationHubBase>> GetNotificationHubContextMock() => new Mock<IHubContext<NotificationHubBase>>();

    private (ConnectedUsersRepository repository, Mock<ICacheService>, Mock<IHubContext<NotificationHubBase>> hubContextMock, Mock<IGroupManager> groupManagerMock, Mock<IHubClients> hubClientsMock) CreateRepositoryAndGetDependencyMocks()
    {
        var cacheServiceMock = TestingUtils.GetCacheServiceMock();
        var groupManagerMock = GetGroupManagerMock();

        var groupManager = groupManagerMock.Object;

        var hubClientsMock = GetHubClientsMock();

        var hubClients = hubClientsMock.Object;

        var hubContextMock = GetNotificationHubContextMock();

        hubContextMock.SetupGet(x => x.Groups).Returns(groupManager);
        hubContextMock.SetupGet(x => x.Clients).Returns(hubClients);

        var repo = new ConnectedUsersRepository(cacheServiceMock.Object, hubContextMock.Object);

        return (repo, cacheServiceMock, hubContextMock, groupManagerMock, hubClientsMock);
    }
}
