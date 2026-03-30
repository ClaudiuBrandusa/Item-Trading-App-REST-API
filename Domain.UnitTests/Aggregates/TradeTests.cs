using Domain.Aggregates.Trades;
using Domain.DomainEvents.Trades;
using Domain.Entities.Identity;
using Domain.Entities.Trades;

namespace Domain.UnitTests.Aggregates;

public class TradeTests
{
    [Fact]
    public void Constructor_InstantiateTrade_ShouldWorkJustFine()
    {
        // Arrange

        string senderUserId = User.GenerateId();
        string receiverUserId = User.GenerateId();
        var expectedSentDate = DateTime.UtcNow;

        // Act

        var trade = new Trade(expectedSentDate, senderUserId, receiverUserId);

        // Assert

        Assert.NotEmpty(trade.TradeId);
        Assert.NotNull(trade.SentTrade);
        Assert.NotNull(trade.ReceivedTrade);
        Assert.Equal(expectedSentDate, trade.SentDate);
        Assert.Equal(senderUserId, trade.SenderId);
        Assert.Equal(receiverUserId, trade.ReceiverId);
        Assert.Empty(trade.TradeContents);
        Assert.Equal(0, trade.GetTotalPrice());
        Assert.Null(trade.Response);
        Assert.Null(trade.ResponseDate);
        var domainEvents = trade.GetDomainEvents();
        Assert.NotNull(domainEvents);
        Assert.Single(domainEvents);
        var domainEvent = domainEvents.First();
        Assert.NotNull(domainEvent);
        var tradeCreatedDomainEvent = Assert.IsAssignableFrom<TradeCreatedDomainEvent>(domainEvent);
        Assert.Equal(trade.TradeId, tradeCreatedDomainEvent.TradeId);
        Assert.Equal(receiverUserId, tradeCreatedDomainEvent.ReceiverId);
    }

    [Fact]
    public void Constructor_InstantiateTradeThatIsAlreadyResponded_ShouldWorkJustFine()
    {
        // Arrange

        string senderUserId = User.GenerateId();
        string receiverUserId = User.GenerateId();
        var expectedSentDate = DateTime.UtcNow;
        var expectedResponseDate = DateTime.UtcNow;
        var expectedResponse = true;

        // Act

        var trade = new Trade(expectedSentDate, senderUserId, receiverUserId, expectedResponseDate, expectedResponse);

        // Assert

        Assert.NotEmpty(trade.TradeId);
        Assert.NotNull(trade.SentTrade);
        Assert.NotNull(trade.ReceivedTrade);
        Assert.Equal(expectedSentDate, trade.SentDate);
        Assert.Equal(senderUserId, trade.SenderId);
        Assert.Equal(receiverUserId, trade.ReceiverId);
        Assert.Empty(trade.TradeContents);
        Assert.Equal(0, trade.GetTotalPrice());
        Assert.Equal(expectedResponse, trade.Response);
        Assert.Equal(expectedResponseDate, trade.ResponseDate);
        var domainEvents = trade.GetDomainEvents();
        Assert.NotNull(domainEvents);
        Assert.Single(domainEvents);
        var domainEvent = domainEvents.First();
        Assert.NotNull(domainEvent);
        var tradeCreatedDomainEvent = Assert.IsAssignableFrom<TradeCreatedDomainEvent>(domainEvent);
        Assert.Equal(trade.TradeId, tradeCreatedDomainEvent.TradeId);
        Assert.Equal(receiverUserId, tradeCreatedDomainEvent.ReceiverId);
    }

    [Fact]
    public void Constructor_InstantiateTradeWithTradeIdGeneratedOutsideOfTheClass_ShouldWorkJustFine()
    {
        // Arrange

        string expectedTradeId = Trade.GenerateId();
        string senderUserId = User.GenerateId();
        string receiverUserId = User.GenerateId();
        var expectedSentDate = DateTime.UtcNow;

        // Act

        var trade = new Trade(expectedTradeId, expectedSentDate, senderUserId, receiverUserId);

        // Assert

        Assert.Equal(expectedTradeId, trade.TradeId);
        Assert.NotNull(trade.SentTrade);
        Assert.NotNull(trade.ReceivedTrade);
        Assert.Equal(expectedSentDate, trade.SentDate);
        Assert.Equal(senderUserId, trade.SenderId);
        Assert.Equal(receiverUserId, trade.ReceiverId);
        Assert.Empty(trade.TradeContents);
        Assert.Equal(0, trade.GetTotalPrice());
        Assert.Null(trade.Response);
        Assert.Null(trade.ResponseDate);
        var domainEvents = trade.GetDomainEvents();
        Assert.NotNull(domainEvents);
        Assert.Empty(domainEvents);
    }

    [Fact]
    public void ClearDomainEvents_InstantiateTrade_ShouldWorkJustFine()
    {
        // Arrange

        string senderUserId = User.GenerateId();
        string receiverUserId = User.GenerateId();
        var expectedSentDate = DateTime.UtcNow;
        var trade = new Trade(expectedSentDate, senderUserId, receiverUserId);

        // Act

        var domainEventsBefore = trade.GetDomainEvents();
        trade.ClearDomainEvents();
        var domainEventsAfter = trade.GetDomainEvents();

        // Assert

        Assert.NotEmpty(domainEventsBefore);
        Assert.Empty(domainEventsAfter);
    }

    [Fact]
    public void AddTradeContent_InstantiateTradeAndAddsTradeContent_ShouldWorkJustFine()
    {
        // Arrange

        string senderUserId = User.GenerateId();
        string receiverUserId = User.GenerateId();
        var expectedSentDate = DateTime.UtcNow;
        var expectedItemId = "1";
        var expectedQuantity = 10;
        var expectedPrice = 10;

        var trade = new Trade(expectedSentDate, senderUserId, receiverUserId);
        trade.ClearDomainEvents();
        var tradeContent = new TradeItem(trade.TradeId, expectedItemId, expectedQuantity, expectedPrice);

        // Act

        trade.AddTradeContent(tradeContent);

        // Assert

        Assert.NotEmpty(trade.TradeContents);
        Assert.Single(trade.TradeContents);
        var retrievedTradeContent = trade.TradeContents.First();
        Assert.NotNull(retrievedTradeContent);
        Assert.Equal(expectedItemId, retrievedTradeContent.ItemId);
        Assert.Equal(trade.TradeId, retrievedTradeContent.TradeId);
        Assert.Equal(expectedQuantity, retrievedTradeContent.Quantity);
        Assert.Equal(expectedPrice, retrievedTradeContent.Price);
        var domainEvents = trade.GetDomainEvents();
        Assert.NotNull(domainEvents);
        Assert.Single(domainEvents);
        var domainEvent = domainEvents.First();
        Assert.NotNull(domainEvent);
        var tradeItemAddedDomainEvent = Assert.IsAssignableFrom<TradeItemAddedDomainEvent>(domainEvent);
        Assert.Equal(trade.TradeId, tradeItemAddedDomainEvent.TradeId);
        Assert.Equal(tradeContent.ItemId, tradeItemAddedDomainEvent.ItemId);
        Assert.Equal(expectedQuantity, tradeItemAddedDomainEvent.Quantity);
        Assert.Equal(expectedPrice, tradeItemAddedDomainEvent.Price);
    }

    [Fact]
    public void AddTradeContent_AddSameTradeItemTwice_ShouldHaveOnlyOneTradeContentEntity()
    {
        // Arrange

        string senderUserId = User.GenerateId();
        string receiverUserId = User.GenerateId();
        var expectedSentDate = DateTime.UtcNow;
        var expectedItemId = "1";
        var expectedQuantity = 10;
        var expectedPrice = 10;

        var trade = new Trade(expectedSentDate, senderUserId, receiverUserId);

        var tradeContent = new TradeItem(trade.TradeId, expectedItemId, expectedQuantity, expectedPrice);

        // Act

        trade.AddTradeContent(tradeContent);
        trade.AddTradeContent(tradeContent);

        // Assert

        Assert.NotEmpty(trade.TradeContents);
        Assert.Single(trade.TradeContents);
        var retrievedTradeContent = trade.TradeContents.First();
        Assert.NotNull(retrievedTradeContent);
        Assert.Equal(expectedItemId, retrievedTradeContent.ItemId);
        Assert.Equal(trade.TradeId, retrievedTradeContent.TradeId);
        Assert.Equal(expectedQuantity * 2, retrievedTradeContent.Quantity);
        Assert.Equal(expectedPrice, retrievedTradeContent.Price);
    }

    [Fact]
    public void RemoveTradeContent_AddTradeContentThenRemove_ShouldWorkJustFine()
    {
        // Arrange

        string senderUserId = User.GenerateId();
        string receiverUserId = User.GenerateId();
        var expectedSentDate = DateTime.UtcNow;
        var expectedItemId = "1";
        var expectedQuantity = 10;
        var expectedPrice = 10;

        var trade = new Trade(expectedSentDate, senderUserId, receiverUserId);

        var tradeContent = new TradeItem(trade.TradeId, expectedItemId, expectedQuantity, expectedPrice);

        // Act

        trade.AddTradeContent(tradeContent);
        trade.RemoveTradeContent(tradeContent);

        // Assert

        Assert.Empty(trade.TradeContents);
    }

    [Fact]
    public void RemoveTradeContent_AddTradeContentThenRemoveByItemId_ShouldWorkJustFine()
    {
        // Arrange

        string senderUserId = User.GenerateId();
        string receiverUserId = User.GenerateId();
        var expectedSentDate = DateTime.UtcNow;
        var expectedItemId = "1";
        var expectedQuantity = 10;
        var expectedPrice = 10;

        var trade = new Trade(expectedSentDate, senderUserId, receiverUserId);

        var tradeContent = new TradeItem(trade.TradeId, expectedItemId, expectedQuantity, expectedPrice);

        // Act

        trade.AddTradeContent(tradeContent);
        trade.RemoveTradeContent(tradeContent.ItemId);

        // Assert

        Assert.Empty(trade.TradeContents);
    }

    [Fact]
    public void RemoveTradeContent_AttemptToRemoveTradeContentThatDoesntExist_ShouldWorkJustFine()
    {
        // Arrange

        string senderUserId = User.GenerateId();
        string receiverUserId = User.GenerateId();
        var expectedSentDate = DateTime.UtcNow;

        var trade = new Trade(expectedSentDate, senderUserId, receiverUserId);

        // Act

        trade.RemoveTradeContent("1");

        // Assert

        Assert.Empty(trade.TradeContents);
    }

    [Fact]
    public void GetTotalPrice_AddTradeContentThenGetTotalPrice_ShouldWorkJustFine()
    {
        // Arrange

        string senderUserId = User.GenerateId();
        string receiverUserId = User.GenerateId();
        var expectedSentDate = DateTime.UtcNow;
        var expectedItemId = "1";
        var expectedQuantity = 10;
        var expectedPrice = 10;
        var expectedTotalPrice = expectedPrice;

        var trade = new Trade(expectedSentDate, senderUserId, receiverUserId);

        var tradeContent = new TradeItem(trade.TradeId, expectedItemId, expectedQuantity, expectedPrice);

        // Act

        trade.AddTradeContent(tradeContent);
        var totalPrice = trade.GetTotalPrice();

        // Assert

        Assert.Equal(expectedTotalPrice, totalPrice);
    }

    [Fact]
    public void GetTotalPrice_TryGettingTotalPriceWithoutAddingAnItem_ShouldWorkJustFine()
    {
        // Arrange

        string senderUserId = User.GenerateId();
        string receiverUserId = User.GenerateId();
        var expectedSentDate = DateTime.UtcNow;
        var expectedTotalPrice = 0;

        var trade = new Trade(expectedSentDate, senderUserId, receiverUserId);

        // Act

        var totalPrice = trade.GetTotalPrice();

        // Assert

        Assert.Equal(expectedTotalPrice, totalPrice);
    }
}