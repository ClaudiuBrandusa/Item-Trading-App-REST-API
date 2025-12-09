using Item_Trading_App_REST_API.Hubs;
using Item_Trading_App_REST_API.Wrappers.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Web.API.UnitTests.Hubs;

public class HubContextWrapperTests
{
    [Fact]
    public void GetClient_Test()
    {
        var expectedClientId = string.Empty;

        (var sut, var hubContextMock, var hubClientsMock, _) = CreateServiceWithDependencies();

        hubClientsMock.Setup(x => x.Group(expectedClientId))
            .Returns(new Mock<IClientProxy>().Object);

        var clients = sut.GetClient(expectedClientId);

        Assert.NotNull(clients);
        hubContextMock.Verify(x => x.Clients, Times.Once);
        hubClientsMock.Verify(x => x.Group(expectedClientId), Times.Once);
    }

    [Fact]
    public void GetClients_Test()
    {
        var expectedClientIds = new string[] { string.Empty, string.Empty };

        (var sut, var hubContextMock, var hubClientsMock, _) = CreateServiceWithDependencies();

        hubClientsMock.Setup(x => x.Groups(expectedClientIds))
            .Returns(new Mock<IClientProxy>().Object);

        var clients = sut.GetClients(expectedClientIds);

        Assert.NotNull(clients);
        hubContextMock.Verify(x => x.Clients, Times.Once);
        hubClientsMock.Verify(x => x.Groups(expectedClientIds), Times.Once);
    }

    [Fact]
    public async Task AddToGroup_Test()
    {
        var expectedConnectionId = string.Empty;
        var expectedGroupName = string.Empty;

        (var sut, var hubContextMock, var hubClientsMock, var groupManagerMock) = CreateServiceWithDependencies();

        groupManagerMock.Setup(x => x.AddToGroupAsync(expectedConnectionId, expectedGroupName, default))
            .Returns(Task.CompletedTask);

        await sut.AddToGroupAsync(expectedConnectionId, expectedGroupName);

        groupManagerMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedGroupName, default), Times.Once);
    }

    [Fact]
    public async Task RemoveFromGroup_Test()
    {
        var expectedConnectionId = string.Empty;
        var expectedGroupName = string.Empty;

        (var sut, var hubContextMock, var hubClientsMock, var groupManagerMock) = CreateServiceWithDependencies();

        groupManagerMock.Setup(x => x.RemoveFromGroupAsync(expectedConnectionId, expectedGroupName, default))
            .Returns(Task.CompletedTask);

        await sut.RemoveFromGroupAsync(expectedConnectionId, expectedGroupName);

        groupManagerMock.Verify(x => x.RemoveFromGroupAsync(expectedConnectionId, expectedGroupName, default), Times.Once);
    }

    [Fact]
    public async Task NotifyUser_Test()
    {
        var expectedConnectionId = string.Empty;
        var expectedGroupName = string.Empty;

        (var sut, var hubContextMock, var hubClientsMock, var groupManagerMock) = CreateServiceWithDependencies();

        var clientProxyMock = new Mock<IClientProxy>();
        var clientProxy = clientProxyMock.Object;

        groupManagerMock.Setup(x => x.AddToGroupAsync(expectedConnectionId, expectedGroupName, default))
            .Returns(Task.CompletedTask);
        hubClientsMock.Setup(x => x.Group(expectedGroupName))
            .Returns(clientProxy);

        var notificationMock = new { };

        await sut.AddToGroupAsync(expectedConnectionId, expectedGroupName);
        await sut.NotifyUserAsync(expectedConnectionId, notificationMock);

        groupManagerMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedGroupName, default), Times.Once);
        hubClientsMock.Verify(x => x.Group(expectedGroupName), Times.Once);
    }

    [Fact]
    public async Task NotifyUsers_Test()
    {
        var expectedConnectionId = string.Empty;
        var expectedGroupName = string.Empty;
        var expectedGroupNames = new string[] { expectedGroupName };

        (var sut, var hubContextMock, var hubClientsMock, var groupManagerMock) = CreateServiceWithDependencies();

        var clientProxyMock = new Mock<IClientProxy>();
        var clientProxy = clientProxyMock.Object;

        groupManagerMock.Setup(x => x.AddToGroupAsync(expectedConnectionId, expectedGroupName, default))
            .Returns(Task.CompletedTask);
        hubClientsMock.Setup(x => x.Groups(expectedGroupNames))
            .Returns(clientProxy);

        var notificationMock = new { };

        await sut.AddToGroupAsync(expectedConnectionId, expectedGroupName);
        await sut.NotifyUsersAsync(expectedGroupNames, notificationMock);

        groupManagerMock.Verify(x => x.AddToGroupAsync(expectedConnectionId, expectedGroupName, default), Times.Once);
        hubClientsMock.Verify(x => x.Groups(expectedGroupNames), Times.Once);
    }

    private (HubContextWrapper, Mock<IHubContext<NotificationHubBase>>, Mock<IHubClients>, Mock<IGroupManager>) CreateServiceWithDependencies()
    {
        var hubClientsMock = new Mock<IHubClients>();
        
        var hubClients = hubClientsMock.Object;

        var groupManagerMock = new Mock<IGroupManager>();

        var groupManager = groupManagerMock.Object;

        var hubContextMock = new Mock<IHubContext<NotificationHubBase>>();

        hubContextMock.SetupGet(x => x.Clients)
            .Returns(hubClients);

        hubContextMock.SetupGet(x => x.Groups)
            .Returns(groupManager);

        var hubContext = hubContextMock.Object;

        var sut = new HubContextWrapper(hubContext);

        return (sut, hubContextMock, hubClientsMock, groupManagerMock);
    }
}