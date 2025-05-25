using Application.Services.Cache;
using Application.Services.ConnectedUsers;
using Domain.Entities.Identity;
using Infrastructure.Services.ConnectedUsers;
using Infrastructure_IntegrationTests.Utils;
using Item_Trading_App_REST_API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Infrastructure_UnitTests.RepositoryTests;

public class ConnectedUsersRepositoryTests
{
    private readonly IConnectedUsersRepository _sut;
    private Dictionary<string, List<string>> groupManagerConnections;
    private readonly Mock<ICacheService> cacheServiceMock;

    public ConnectedUsersRepositoryTests()
    {
        groupManagerConnections = new();
        var hubContextMock = GetHubContextMock(groupManagerConnections);
        
        cacheServiceMock = TestingUtils.GetCacheServiceMock();

        cacheServiceMock.Setup(x => x.SetCacheValueAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string key, string value) => Task.CompletedTask);

        cacheServiceMock.Setup(x => x.ClearCacheKeyAsync(It.IsAny<string>()))
            .Returns((string key) => Task.CompletedTask);
        
        _sut = new ConnectedUsersRepository(cacheServiceMock.Object, hubContextMock.Object);
    }

    [Fact(DisplayName = "Add connection id to user then check if it was added")]
    public async Task AddConnectionIdToUser()
    {
        // Arrange

        string connectionId = Guid.NewGuid().ToString();
        string userId = User.GenerateId();
        string userName = "UserName_0";

        // Act

        var userExistBeforeTest = _sut.UserExist(userId);
        await _sut.AddConnectionIdToUser(connectionId, userId, userName);
        var userExistAfterTest = _sut.UserExist(userId);

        // Assert

        Assert.False(userExistBeforeTest);
        Assert.True(userExistAfterTest);
        Assert.Contains(userId, groupManagerConnections.Keys);
    }

    [Fact(DisplayName = "Remove connection id from user the check if it was removed")]
    public async Task RemoveConnectionIdFromUser()
    {
        // Arrange

        string connectionId = Guid.NewGuid().ToString();
        string userId = User.GenerateId();
        string userName = "UserName_1";

        // Act

        var userExistBeforeTest = _sut.UserExist(userId);
        await _sut.AddConnectionIdToUser(connectionId, userId, userName);
        var userWasAddedSuccessfully = _sut.UserExist(userId);
        await _sut.RemoveConnectionIdFromUser(connectionId, userId);
        var userDeletedSuccessfully = !_sut.UserExist(userId);

        // Assert

        Assert.False(userExistBeforeTest);
        Assert.True(userWasAddedSuccessfully);
        Assert.True(userDeletedSuccessfully);
    }

    [Fact(DisplayName = "Add several users then list the active users ids")]
    public async Task GetActiveUserIds()
    {
        // Arrange

        const int addedUsersAmount = 3;
        const int startIndex = 2;

        var groupManagerConnections = new Dictionary<string, List<string>>();
        var hubContextMock = GetHubContextMock(groupManagerConnections);

        var isolatedSUT = new ConnectedUsersRepository(cacheServiceMock.Object, hubContextMock.Object);

        var addedUserIds = new string[addedUsersAmount];

        for (int i = 0; i < addedUsersAmount; i++)
        {
            string userId = User.GenerateId();
            string connectionId = Guid.NewGuid().ToString();
            string userName = $"UserName_{i + startIndex}";

            addedUserIds[i] = userId;

            await isolatedSUT.AddConnectionIdToUser(connectionId, userId, userName);
        }

        // Act

        var activeUserIds = isolatedSUT.GetActiveUserIds();

        // Assert

        Assert.All(addedUserIds, addedUserId => activeUserIds.Contains(addedUserId));
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

    #endregion Utils
}
