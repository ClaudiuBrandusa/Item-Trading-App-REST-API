using Application.Services.Cache;
using Infrastructure.Services.ConnectedUsers;
using Infrastructure.Wrappers.Hubs;
using Infrastructure_IntegrationTests.Utils;
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
            var hubContextMock
        ) = CreateRepositoryAndGetDependencyMocks();

        var result = await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        var connectionsForUserId = sut.ListConnectionIdsForUserId(expectedUserId);

        hubContextMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);

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
            var hubContextMock
        ) = CreateRepositoryAndGetDependencyMocks();

        var addConnectionResult = await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        await sut.RemoveConnectionIdFromUser(expectedConnectionId, expectedUserId);
        var connectionsForUserId = sut.ListConnectionIdsForUserId(expectedUserId);

        hubContextMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);
        hubContextMock.Verify(x => x.RemoveFromGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);

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
            var hubContextMock
        ) = CreateRepositoryAndGetDependencyMocks();

        var notificationMock = new { };

        var addConnectionResult = await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        await sut.NotifyUserAsync(expectedUserId, notificationMock);
        
        hubContextMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);
        hubContextMock.Verify(x => x.NotifyUserAsync(expectedUserId, notificationMock), Times.Once);
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
            var hubContextMock
        ) = CreateRepositoryAndGetDependencyMocks();

        var notificationMock = new { };

        var addConnectionResult = await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        await sut.NotifyUsersAsync(notificationMock);

        hubContextMock.Verify(x => x.NotifyUsersAsync(expectedUserIds, notificationMock), Times.Once);
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
            var hubContextMock
        ) = CreateRepositoryAndGetDependencyMocks();

        var notificationMock = new { };

        var clientProxyMock = new Mock<IClientProxy>();
        var clientProxy = clientProxyMock.Object;

        var addConnectionResult = await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        await sut.NotifyUsersAsync(expectedUserIds, notificationMock);

        hubContextMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);
        hubContextMock.Verify(x => x.NotifyUsersAsync(expectedUserIds, notificationMock), Times.Once);
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
            var hubContextMock
        ) = CreateRepositoryAndGetDependencyMocks();

        var notificationMock = new { };

        await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        await sut.AddConnectionIdToUser(expectedConnectionId, exceptedUserId, expectedUserName);
        await sut.NotifyAllUsersExceptAsync(exceptedUserId, notificationMock);

        hubContextMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);
        hubContextMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, exceptedUserId, CancellationToken.None), Times.Once);
        hubContextMock.Verify(x => x.NotifyUsersAsync(expectedUserIds, notificationMock), Times.Once);
    }

    private (ConnectedUsersRepository repository, Mock<ICacheService>, Mock<IHubContextWrapper> hubContextWrapperMock) CreateRepositoryAndGetDependencyMocks()
    {
        var cacheServiceMock = TestingUtils.GetCacheServiceMock();
        
        var hubContextWrapperMock = new Mock<IHubContextWrapper>();

        var repo = new ConnectedUsersRepository(cacheServiceMock.Object, hubContextWrapperMock.Object);

        return (repo, cacheServiceMock, hubContextWrapperMock);
    }
}
