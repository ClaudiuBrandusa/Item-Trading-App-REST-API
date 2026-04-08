using Application.Services.Cache;
using CommonTestUtils.MockedServices;
using Infrastructure.Services.ConnectedUsers;
using Infrastructure.Wrappers.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Infrastructure_UnitTests.RepositoryTests;

public class ConnectedUsersRepositoryTests
{
    private readonly ConnectedUsersRepository _sut;

    public ConnectedUsersRepositoryTests()
    {
        _sut = CreateRepository();
    }

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

    [Fact]
    public async Task AddConnectionIdToUser_AddTwoUsersAtTheSameTime_ShouldBeFine()
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

        await Task.WhenAll(
            sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName),
            sut.AddConnectionIdToUser(expectedConnectionId, exceptedUserId, expectedUserName)
        );

        await sut.NotifyAllUsersExceptAsync(exceptedUserId, notificationMock);
    }

    [Fact]
    public async Task AddConnectionIdToUser_AddConnectionIdsInParallel_ShouldExecuteCorrectly()
    {
        var sut = _sut;

        var expectedUsersCount = 200_000;

        var tasks = new Task[expectedUsersCount];

        Parallel.For(0, expectedUsersCount, (index) =>
        {
            var func = async () =>
            {
                var connectionId = Guid.NewGuid().ToString();
                var userId = Guid.NewGuid().ToString();
                var userName = Guid.NewGuid().ToString();

                await sut.AddConnectionIdToUser(connectionId, userId, userName);
            };

            tasks[index] = func.Invoke();
        });

        await Task.WhenAll(tasks);

        var userIds = sut.ListUserIds();
        var distinctUserIds = userIds.Distinct().ToArray();

        Assert.NotEmpty(distinctUserIds);
        Assert.Equal(userIds.Length, distinctUserIds.Length); // no duplicates
        Assert.Equal(expectedUsersCount, distinctUserIds.Length);
        Assert.All(userIds, userId =>
        {
            Assert.NotEmpty(userId);

            var connectionIds = sut.ListConnectionIdsForUserId(userId);

            Assert.Single(connectionIds);
            Assert.NotEmpty(connectionIds[0]);
        });
    }

    [Fact]
    public async Task AddConnectionIdToUser_AddMultipleConnectionIdsInParallel_ShouldExecuteCorrectly()
    {
        var sut = _sut;

        var expectedUsersCount = 1000;

        var tasks = new Task[expectedUsersCount];

        Parallel.For(0, expectedUsersCount, (index) =>
        {
            var func = async () =>
            {
                var connectionId0 = Guid.NewGuid().ToString();
                var connectionId1 = Guid.NewGuid().ToString();
                var connectionId2 = Guid.NewGuid().ToString();
                var userId = Guid.NewGuid().ToString();
                var userName = Guid.NewGuid().ToString();

                await Task.WhenAll(
                    sut.AddConnectionIdToUser(connectionId0, userId, userName),
                    sut.AddConnectionIdToUser(connectionId1, userId, userName),
                    sut.AddConnectionIdToUser(connectionId2, userId, userName)
                );
            };

            tasks[index] = func.Invoke();
        });

        await Task.WhenAll(tasks);

        var userIds = sut.ListUserIds();
        var distinctUserIds = userIds.Distinct().ToArray();

        Assert.NotEmpty(distinctUserIds);
        Assert.Equal(userIds.Length, distinctUserIds.Length); // no duplicates
        Assert.Equal(expectedUsersCount, distinctUserIds.Length);
        Assert.All(userIds, userId =>
        {
            Assert.NotEmpty(userId);

            var connectionIds = sut.ListConnectionIdsForUserId(userId);

            Assert.Equal(3, connectionIds.Length);
            Assert.NotEmpty(connectionIds[0]);
            Assert.NotEmpty(connectionIds[1]);
            Assert.NotEmpty(connectionIds[2]);
            Assert.Distinct(connectionIds);
        });
    }

    private (ConnectedUsersRepository repository, Mock<ICacheService>, Mock<IHubContextWrapper> hubContextWrapperMock) CreateRepositoryAndGetDependencyMocks()
    {
        var cacheServiceMock = CacheUtils.GetCacheServiceMock();
        
        var hubContextWrapperMock = new Mock<IHubContextWrapper>();

        var repo = new ConnectedUsersRepository(cacheServiceMock.Object, hubContextWrapperMock.Object);

        return (repo, cacheServiceMock, hubContextWrapperMock);
    }

    private ConnectedUsersRepository CreateRepository()
    {
        var cacheServiceMock = CacheUtils.GetCacheServiceMock();

        var hubContextWrapperMock = new Mock<IHubContextWrapper>();

        var repo = new ConnectedUsersRepository(cacheServiceMock.Object, hubContextWrapperMock.Object);

        return repo;
    }
}
