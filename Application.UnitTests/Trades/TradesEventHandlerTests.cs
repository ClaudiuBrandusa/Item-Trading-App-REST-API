using Application.Behaviors.Identity.GetUsername;
using Application.Behaviors.Inventories.DropItem;
using Application.Behaviors.Inventories.LockItem;
using Application.Behaviors.Inventories.UnlockItem;
using Application.Behaviors.Trade.AddTradeItem;
using Application.Behaviors.Trade.CancelTrade;
using Application.Behaviors.Trade.CreateTrade;
using Application.Behaviors.Trade.RemoveTradeItem;
using Application.Behaviors.Trade.RespondTrade;
using Application.Constants;
using Application.Models.Inventories;
using Application.Models.Trades;
using Application.Services.Cache;
using Application.Services.Notification;
using Application.Utils.Notifications.Strategies;
using Domain.DomainEvents.Inventories;
using Domain.DomainEvents.Trades;
using Domain.Entities.Items;
using MediatR;

namespace Application_UnitTests.Trades;

public class TradesEventHandlerTests
{
    [Fact]
    public async Task TradeCreated_TradeWasCreated_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedUserId = "user-id";
        var expectedUsername = "user-name";
        var expectedTradeId = "trade-id";

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<GetUsernameQuery>(), CancellationToken.None))
            .ReturnsAsync((GetUsernameQuery query, CancellationToken ct) => expectedUsername);

        var mediator = mediatorMock.Object;

        var eventHandler = new TradeCreatedDomainEventHandler(clientNotificationService, mediator);

        var notificationMock = new TradeCreatedDomainEvent(expectedTradeId, expectedUserId);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);
        var approximateDateTime = DateTime.UtcNow;

        // Assert
        
        clientNotificationServiceMock.Verify(x => x.SendMessageNotificationAsync(
            It.IsAny<NotifySingleUserStrategy>(),
            It.IsAny<string>(),
            It.Is((DateTime time) => (time - approximateDateTime).TotalSeconds < 5) // shouldn't take longer than five seconds
        ), Times.Once);
        
        clientNotificationServiceMock.Verify(x => x.SendCreatedNotificationAsync(
            It.IsAny<NotifySingleUserStrategy>(),
            It.Is((string y) => y == NotificationCategoryTypes.Trade),
            It.Is((string y) => y == expectedTradeId),
            It.Is((object? customData) => customData == null)
        ), Times.Once);
    }

    [Fact]
    public async Task TradeItemAdded_TradeItemWasAdded_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedTradeId = "trade-id";
        var expectedItemId = "item-id";
        var expectedQuantity = 10;
        var expectedPrice = 10;

        var cacheServiceMock = new Mock<ICacheService>();

        var cacheService = cacheServiceMock.Object;

        var eventHandler = new TradeItemAddedEventHandler(cacheService);

        var notificationMock = new TradeItemAddedDomainEvent(expectedTradeId, expectedItemId, expectedQuantity, expectedPrice);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);
        
        // Assert
        
        cacheServiceMock.Verify(x => x.SetCacheValueAsync(
            It.Is<string>(y => y == CacheKeys.TradeItem.GetTradeItemKey(expectedTradeId, expectedItemId)),
            It.Is((object? y) =>
                y != null &&
                expectedTradeId == GetPropertyValueFromObject(y, "TradeId").ToString() &&
                expectedItemId == GetPropertyValueFromObject(y, "ItemId").ToString() &&
                expectedQuantity == int.Parse(GetPropertyValueFromObject(y, "Quantity").ToString()!) &&
                expectedPrice == int.Parse(GetPropertyValueFromObject(y, "Price").ToString()!)
            )
        ), Times.Once);
        
        cacheServiceMock.Verify(x => x.AddToSet(
            It.Is<string>(y => y == CacheKeys.UsedItem.GetUsedItemKey(expectedItemId)),
            It.Is<string>((y) => y == expectedTradeId)
        ), Times.Once);
    }

    [Fact]
    public async Task TradeItemRemoved_TradeItemWasRemoved_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedTradeId = "trade-id";
        var expectedItemId = "item-id";

        var cacheServiceMock = new Mock<ICacheService>();

        var cacheService = cacheServiceMock.Object;

        var eventHandler = new TradeItemRemovedEventHandler(cacheService);

        var notificationMock = new TradeItemRemovedDomainEvent(expectedTradeId, expectedItemId);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);
        
        // Assert
        
        cacheServiceMock.Verify(x => x.ClearCacheKeyAsync(
            It.Is<string>(y => y == CacheKeys.TradeItem.GetTradeItemKey(expectedTradeId, expectedItemId))
        ), Times.Once);
        
        cacheServiceMock.Verify(x => x.RemoveFromSet(
            It.Is<string>(y => y == CacheKeys.UsedItem.GetUsedItemKey(expectedItemId)),
            It.Is<string>((y) => y == expectedTradeId)
        ), Times.Once);
    }

    [Fact]
    public async Task TradeResponded_TradeAccepted_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedTradeId = "trade-id";
        var expectedItemId = "item-id";
        var expectedResponse = true;

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new TradeRespondedDomainEventHandler(clientNotificationService);

        var notificationMock = new TradeRespondedDomainEvent(expectedTradeId, expectedItemId, expectedResponse);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);
        var approximateDateTime = DateTime.UtcNow;
        
        // Assert
        
        clientNotificationServiceMock.Verify(x => x.SendUpdatedNotificationAsync(
            It.IsAny<NotifySingleUserStrategy>(),
            NotificationCategoryTypes.Trade,
            expectedTradeId,
            It.Is<RespondedTradeNotification>((y) => expectedResponse == y.Response)
        ), Times.Once);
    }

    [Fact]
    public async Task TradeResponded_TradeRejected_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedTradeId = "trade-id";
        var expectedItemId = "item-id";
        var expectedResponse = false;

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new TradeRespondedDomainEventHandler(clientNotificationService);

        var notificationMock = new TradeRespondedDomainEvent(expectedTradeId, expectedItemId, expectedResponse);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);
        var approximateDateTime = DateTime.UtcNow;
        
        // Assert
        
        clientNotificationServiceMock.Verify(x => x.SendUpdatedNotificationAsync(
            It.IsAny<NotifySingleUserStrategy>(),
            NotificationCategoryTypes.Trade,
            expectedTradeId,
            It.Is<RespondedTradeNotification>((y) => expectedResponse == y.Response)
        ), Times.Once);
    }

    [Fact]
    public async Task TradeCanceled_TradeRejected_ShouldProcessSuccessfully()
    {
        // Arrange

        var expectedTradeId = "trade-id";
        var expectedItemId = "item-id";

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        var clientNotificationService = clientNotificationServiceMock.Object;

        var eventHandler = new TradeCancelledDomainEventHandler(clientNotificationService);

        var notificationMock = new TradeCancelledDomainEvent(expectedTradeId, expectedItemId);

        // Act

        await eventHandler.Handle(notificationMock, CancellationToken.None);
        var approximateDateTime = DateTime.UtcNow;
        
        // Assert
        
        clientNotificationServiceMock.Verify(x => x.SendUpdatedNotificationAsync(
            It.IsAny<NotifySingleUserStrategy>(),
            NotificationCategoryTypes.Trade,
            expectedTradeId,
            It.Is<RespondedTradeNotification>((y) => null == y.Response)
        ), Times.Once);
    }

    private static object GetPropertyValueFromObject(object source, string propertyName) =>
        source.GetType()
            .GetProperty(propertyName)!
            .GetValue(source)!;
}
