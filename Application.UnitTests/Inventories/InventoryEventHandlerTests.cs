using Application.Behaviors.Inventories.AddItem;
using Application.Behaviors.Inventories.DropItem;
using Application.Behaviors.Inventories.LockItem;
using Application.Behaviors.Inventories.UnlockItem;
using Application.Constants;
using Application.Models.Inventories;
using Application.Services.Notification;
using Application.Utils.Notifications.Strategies;
using Domain.DomainEvents.Inventories;
using Domain.Entities.Items;

namespace Application_UnitTests.Items;

public class TradesEventHandlerTests
{
    [Fact]
    public async Task InventoryItemAdded_AnItemWasAdded_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedUserId = "user-id";
        var expectedQuantity = 10;
        var expectedNotifyFlag = true;

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new InventoryItemAddedEventHandler(clientNotificationService);

        var item = new Item("Gold");

        var notificationMock = new InventoryItemAddedDomainEvent(expectedUserId, item.ItemId, expectedQuantity, expectedNotifyFlag);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);

        // Assert
        
        clientNotificationServiceMock.Verify(x => x.SendUpdatedNotificationAsync(
            It.IsAny<NotifySingleUserStrategy>(),
            It.Is((string y) => y == NotificationCategoryTypes.Inventory),
            It.Is((string y) => y == item.ItemId),
            It.Is((InventoryItemQuantityNotification? y) =>
                y!.AddAmount == true &&
                y.Amount == expectedQuantity
            )
        ), Times.Once);
    }

    [Fact]
    public async Task InventoryItemAdded_AnItemWasAddedButNoNotificationExpected_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedUserId = "user-id";
        var expectedQuantity = 10;
        var expectedNotifyFlag = false;

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new InventoryItemAddedEventHandler(clientNotificationService);

        var item = new Item("Gold");

        var notificationMock = new InventoryItemAddedDomainEvent(expectedUserId, item.ItemId, expectedQuantity, expectedNotifyFlag);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);

        // Assert
        
        clientNotificationServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InventoryItemDropped_AnItemWasDropped_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedUserId = "user-id";
        var expectedQuantity = 10;
        var expectedNotifyFlag = true;

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new InventoryItemDroppedEventHandler(clientNotificationService);

        var item = new Item("Gold");

        var notificationMock = new InventoryItemDroppedDomainEvent(expectedUserId, item.ItemId, expectedQuantity, expectedNotifyFlag);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);

        // Assert
        
        clientNotificationServiceMock.Verify(x => x.SendUpdatedNotificationAsync(
            It.IsAny<NotifySingleUserStrategy>(),
            It.Is((string y) => y == NotificationCategoryTypes.Inventory),
            It.Is((string y) => y == item.ItemId),
            It.Is((InventoryItemQuantityNotification? y) =>
                y!.AddAmount == false &&
                y.Amount == expectedQuantity
            )
        ), Times.Once);
    }

    [Fact]
    public async Task InventoryItemDropped_AnItemWasDroppedButNoNotificationExpected_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedUserId = "user-id";
        var expectedQuantity = 10;
        var expectedNotifyFlag = false;

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new InventoryItemDroppedEventHandler(clientNotificationService);

        var item = new Item("Gold");

        var notificationMock = new InventoryItemDroppedDomainEvent(expectedUserId, item.ItemId, expectedQuantity, expectedNotifyFlag);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);

        // Assert
        
        clientNotificationServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InventoryItemLocked_AnItemWasLocked_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedUserId = "user-id";
        var expectedQuantity = 10;
        var expectedNotifyFlag = true;

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new InventoryItemLockedEventHandler(clientNotificationService);

        var item = new Item("Gold");

        var notificationMock = new InventoryItemLockedDomainEvent(expectedUserId, item.ItemId, expectedQuantity, expectedNotifyFlag);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);

        // Assert
        
        clientNotificationServiceMock.Verify(x => x.SendUpdatedNotificationAsync(
            It.IsAny<NotifySingleUserStrategy>(),
            It.Is((string y) => y == NotificationCategoryTypes.Inventory),
            It.Is((string y) => y == item.ItemId),
            It.Is((InventoryItemQuantityNotification? y) =>
                y!.AddAmount == true &&
                y.Amount == expectedQuantity
            )
        ), Times.Once);
    }

    [Fact]
    public async Task InventoryItemLocked_AnItemWasLockedButNoNotificationExpected_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedUserId = "user-id";
        var expectedQuantity = 10;
        var expectedNotifyFlag = false;

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new InventoryItemLockedEventHandler(clientNotificationService);

        var item = new Item("Gold");

        var notificationMock = new InventoryItemLockedDomainEvent(expectedUserId, item.ItemId, expectedQuantity, expectedNotifyFlag);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);

        // Assert
        
        clientNotificationServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InventoryItemUnlocked_AnItemWasUnlocked_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedUserId = "user-id";
        var expectedQuantity = 10;
        var expectedNotifyFlag = true;

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new InventoryItemUnlockedEventHandler(clientNotificationService);

        var item = new Item("Gold");

        var notificationMock = new InventoryItemUnlockedDomainEvent(expectedUserId, item.ItemId, expectedQuantity, expectedNotifyFlag);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);

        // Assert
        
        clientNotificationServiceMock.Verify(x => x.SendUpdatedNotificationAsync(
            It.IsAny<NotifySingleUserStrategy>(),
            It.Is((string y) => y == NotificationCategoryTypes.Inventory),
            It.Is((string y) => y == item.ItemId),
            It.Is((InventoryItemQuantityNotification? y) =>
                y!.AddAmount == true &&
                y.Amount == expectedQuantity
            )
        ), Times.Once);
    }

    [Fact]
    public async Task InventoryItemUnlocked_AnItemWasUnlockedButNoNotificationExpected_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedUserId = "user-id";
        var expectedQuantity = 10;
        var expectedNotifyFlag = false;

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new InventoryItemUnlockedEventHandler(clientNotificationService);

        var item = new Item("Gold");

        var notificationMock = new InventoryItemUnlockedDomainEvent(expectedUserId, item.ItemId, expectedQuantity, expectedNotifyFlag);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);

        // Assert
        
        clientNotificationServiceMock.VerifyNoOtherCalls();
    }
}
