using Application.Behaviors.Inventories.ListUsersOwningItem;
using Application.Behaviors.Inventories.RemoveItemFromUsers;
using Application.Behaviors.Item.CreateItem;
using Application.Behaviors.Item.DeleteItem;
using Application.Behaviors.Item.UpdateItem;
using Application.Constants;
using Application.Models.Common;
using Application.Models.Inventories;
using Application.Services.Notification;
using Application.Utils.Notifications.Strategies;
using Domain.DomainEvents.Items;
using Domain.Entities.Identity;
using Domain.Entities.Items;
using MediatR;

namespace Application_UnitTests.Items;

public class ItemEventHandlerTests
{
    [Fact]
    public async Task ItemCreated_AnItemWasCreated_ShouldProcessSuccessfully()
    {
        // Arrange

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new ItemCreatedEventHandler(clientNotificationService);

        var item = new Item("Gold");
        var expectedSenderId = User.GenerateId();

        var notificationMock = new ItemCreatedDomainEvent(item.ItemId, expectedSenderId);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);

        // Assert
        
        clientNotificationServiceMock.Verify(x => x.SendCreatedNotificationAsync(
            It.IsAny<NotifyAllUsersExceptStrategy>(),
            It.Is((string y) => y == NotificationCategoryTypes.Item),
            It.Is((string y) => y == item.ItemId),
            It.Is((object? y) => y == null)
        ), Times.Once);
    }

    [Fact]
    public async Task ItemUpdated_AnItemWasUpdated_ShouldProcessSuccessfully()
    {
        // Arrange

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new ItemUpdatedEventHandler(clientNotificationService);

        var item = new Item("Gold");
        var expectedSenderId = User.GenerateId();

        var notificationMock = new ItemUpdatedDomainEvent(item.ItemId, expectedSenderId);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);

        // Assert
        
        clientNotificationServiceMock.Verify(x => x.SendUpdatedNotificationAsync(
            It.IsAny<NotifyAllUsersExceptStrategy>(),
            It.Is((string y) => y == NotificationCategoryTypes.Item),
            It.Is((string y) => y == item.ItemId),
            It.Is((object? y) => y == null)
        ), Times.Once);
    }

    [Fact]
    public async Task ItemDelete_AnItemWasDeleted_ShouldProcessSuccessfully()
    {
        // Arrange

        var item = new Item("Gold");
        var expectedSenderId = User.GenerateId();
        var expectedUserId = "user-id";
        
        var expectedUsersOwningResult = Result<UsersOwningItem>.Success(new UsersOwningItem
        {
            ItemId = item.ItemId,
            UserIds = [expectedUserId]
        });

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<GetUserIdsOwningItemQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUserIdsOwningItemQuery query, CancellationToken ct) => expectedUsersOwningResult);

        var mediator = mediatorMock.Object;

        var eventHandler = new ItemDeletedEventHandler(clientNotificationService, mediator);

        var notificationMock = new ItemDeletedDomainEvent(item.ItemId, expectedSenderId);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);

        // Assert
        
        clientNotificationServiceMock.Verify(x => x.SendDeletedNotificationAsync(
            It.IsAny<NotifyAllUsersExceptStrategy>(),
            It.Is((string y) => y == NotificationCategoryTypes.Item),
            It.Is((string y) => y == item.ItemId),
            It.Is((object? y) => y == null)
        ), Times.Once);

        mediatorMock.Verify(x => x.Send(
            It.Is((GetUserIdsOwningItemQuery y) => y.ItemId == item.ItemId),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        mediatorMock.Verify(x => x.Send(
            It.Is((RemoveItemFromUsersCommand y) => 
                y.ItemId == item.ItemId &&
                y.UserIds == expectedUsersOwningResult.Content!.UserIds
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
