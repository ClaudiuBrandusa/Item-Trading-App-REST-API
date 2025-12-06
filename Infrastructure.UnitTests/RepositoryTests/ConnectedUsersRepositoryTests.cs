using Application.Services.Cache;
using Application.Services.ConnectedUsers;
using Infrastructure.Services.ConnectedUsers;
using Infrastructure_IntegrationTests.Utils;
using Item_Trading_App_REST_API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Infrastructure_UnitTests.RepositoryTests;

public class ConnectedUsersRepositoryTests
{
    private readonly IConnectedUsersRepository _sut;

    public ConnectedUsersRepositoryTests()
    {
        var cacheServiceMock = TestingUtils.GetCacheServiceMock();
        var groupManagerMock = GetGroupManagerMock();

        var groupManager = groupManagerMock.Object;

        var signalRHubMock = GetNotificationHubContextMock();
        
        signalRHubMock.SetupGet(x => x.Groups).Returns(groupManager);

        _sut = new ConnectedUsersRepository(cacheServiceMock.Object, signalRHubMock.Object);
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
            var hubContextMock,
            var groupManagerMock
        ) = CreateRepositoryAndGetDependencyMocks();

        var result = await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        var connectionsForUserId = sut.ListConnectionIdsForUserId(expectedUserId);

        groupManagerMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);

        Assert.NotNull(connectionsForUserId);
        Assert.Contains(expectedConnectionId, connectionsForUserId);
        //Assert.True(result);
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
            var groupManagerMock
        ) = CreateRepositoryAndGetDependencyMocks();

        var addConnectionResult = await sut.AddConnectionIdToUser(expectedConnectionId, expectedUserId, expectedUserName);
        await sut.RemoveConnectionIdFromUser(expectedConnectionId, expectedUserId);
        var connectionsForUserId = sut.ListConnectionIdsForUserId(expectedUserId);

        groupManagerMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedUserId, CancellationToken.None), Times.Once);

        Assert.NotNull(connectionsForUserId);
        Assert.Empty(connectionsForUserId);
    }

    private Mock<IGroupManager> GetGroupManagerMock() => new Mock<IGroupManager>();

    private Mock<IHubContext<NotificationHubBase>> GetNotificationHubContextMock() => new Mock<IHubContext<NotificationHubBase>> ();

    private (ConnectedUsersRepository repository, Mock<ICacheService>, Mock<IHubContext<NotificationHubBase>> hubContextMock, Mock<IGroupManager> groupManagerMock) CreateRepositoryAndGetDependencyMocks()
    {
        var cacheServiceMock = TestingUtils.GetCacheServiceMock();
        var groupManagerMock = GetGroupManagerMock();

        var groupManager = groupManagerMock.Object;

        var hubContextMock = GetNotificationHubContextMock();

        hubContextMock.SetupGet(x => x.Groups).Returns(groupManager);

        var repo = new ConnectedUsersRepository(cacheServiceMock.Object, hubContextMock.Object);

        return (repo, cacheServiceMock, hubContextMock, groupManagerMock);
    }
}
