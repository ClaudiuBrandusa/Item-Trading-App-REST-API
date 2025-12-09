using Item_Trading_App_REST_API.Hubs;
using Item_Trading_App_REST_API.Wrappers.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Web.API.UnitTests.Hubs;

public class HubContextWrapperTests
{
    [Fact]
    public void GetClients_Test()
    {
        var expectedClientIds = new string[] { string.Empty, string.Empty };

        (var sut, var hubContextMock, var hubClientsMock) = CreateServiceWithDependencies();

        hubClientsMock.Setup(x => x.Groups(expectedClientIds))
            .Returns(new Mock<IClientProxy>().Object);

        var clients = sut.GetClients(expectedClientIds);

        Assert.NotNull(clients);
        hubContextMock.Verify(x => x.Clients, Times.Once);
        hubClientsMock.Verify(x => x.Groups(expectedClientIds), Times.Once);
    }

    private (HubContextWrapper, Mock<IHubContext<NotificationHubBase>>, Mock<IHubClients>) CreateServiceWithDependencies()
    {
        var hubClientsMock = new Mock<IHubClients>();
        
        var hubClients = hubClientsMock.Object;

        var hubContextMock = new Mock<IHubContext<NotificationHubBase>>();

        hubContextMock.SetupGet(x => x.Clients)
            .Returns(hubClients);

        var hubContext = hubContextMock.Object;

        var sut = new HubContextWrapper(hubContext);

        return (sut, hubContextMock, hubClientsMock);
    }
}