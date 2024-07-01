using Domain.Repositories;
using Domain.Trades;
using Infrastructure.Repositories;
using Infrastructure.Services.DatabaseContextWrapper;
using Infrastructure_IntegrationTests.Utils;
using MediatR;
using Moq;

namespace Infrastructure_UnitTests;

public class TradeRepositoryTests
{
    private readonly ITradeRepository _sut;
    private readonly string DEFAULT_TRADE_ID = Guid.NewGuid().ToString();
    private readonly string DEFAULT_SENDER_ID = Guid.NewGuid().ToString();
    private readonly string DEFAULT_RECEIVER_ID = Guid.NewGuid().ToString();

    private IDatabaseContextWrapper _contextWrapper;

    public TradeRepositoryTests()
    {
        _contextWrapper = TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());
        var cacheServiceMock = TestingUtils.GetCacheServiceMock();
        var senderMock = new Mock<ISender>();

        _sut = new TradeRepository(_contextWrapper, cacheServiceMock.Object, senderMock.Object);
    }

    [Fact(DisplayName = "Create a trade and return the created trade")]
    public async Task GetTradeEntity_CreateTradeAndReturnTrade_ReturnsTheCreatedTrade()
    {
        // Arrange

        var tradeMock = new Trade
        {
            TradeId = DEFAULT_TRADE_ID,
            SentDate = DateTime.UtcNow
        };

        var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

        // Act

        var tradeResult = await _sut.GetTradeEntityAsync(tradeMock.TradeId);

        // Assert

        Assert.NotNull(tradeResult);
        Assert.Equal(tradeMock.TradeId, tradeResult.TradeId);
        Assert.Equal(tradeMock.Response, tradeResult.Response);
        Assert.Equal(tradeMock.SentDate, tradeResult.SentDate);
        Assert.Equal(tradeMock.ResponseDate, tradeResult.ResponseDate);
    }

    [Fact(DisplayName = "Create a trade and return the created trade (cached)")]
    public async Task GetCachedTrade_CreateTradeAndReturnTrade_ReturnsTheCachedCreatedTrade()
    {
        // Arrange

        var tradeMock = new Trade
        {
            TradeId = DEFAULT_TRADE_ID,
            SentDate = DateTime.UtcNow
        };

        var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

        await _sut.AddSentAndReceivedTradeEntitiesAsync(DEFAULT_TRADE_ID, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var tradeResult = await _sut.GetCachedTradeAsync(tradeMock.TradeId);
        
        // Assert

        Assert.NotNull(tradeResult);
        Assert.Equal(tradeMock.TradeId, tradeResult.TradeId);
        Assert.Equal(tradeMock.Response, tradeResult.Response);
        Assert.Equal(tradeMock.SentDate, tradeResult.SentDate);
        Assert.Equal(tradeMock.ResponseDate, tradeResult.ResponseDate);
        Assert.Equal(DEFAULT_SENDER_ID, tradeResult.SenderUserId);
        Assert.Equal(DEFAULT_RECEIVER_ID, tradeResult.ReceiverUserId);
    }

    [Fact(DisplayName = "Create a trade and return the sent trade")]
    public async Task GetSentTrade_CreateTradeAndReturnTrade_ReturnsTheSentCreatedTrade()
    {
        // Arrange

        var tradeMock = new Trade
        {
            TradeId = DEFAULT_TRADE_ID,
            SentDate = DateTime.UtcNow
        };

        var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

        await _sut.AddSentAndReceivedTradeEntitiesAsync(DEFAULT_TRADE_ID, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var sentTradeResult = await _sut.GetSentTradeEntityAsync(tradeMock.TradeId);

        // Assert

        Assert.NotNull(sentTradeResult);
        Assert.Equal(tradeMock.TradeId, sentTradeResult.TradeId);
        Assert.Equal(DEFAULT_SENDER_ID, sentTradeResult.SenderId);
    }

    [Fact(DisplayName = "Create a trade and return the received trade")]
    public async Task GetReceivedTrade_CreateTradeAndReturnTrade_ReturnsTheReceivedCreatedTrade()
    {
        // Arrange

        var tradeMock = new Trade
        {
            TradeId = DEFAULT_TRADE_ID,
            SentDate = DateTime.UtcNow
        };

        var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

        await _sut.AddSentAndReceivedTradeEntitiesAsync(DEFAULT_TRADE_ID, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var sentTradeResult = await _sut.GetReceivedTradeEntityAsync(tradeMock.TradeId);

        // Assert

        Assert.NotNull(sentTradeResult);
        Assert.Equal(tradeMock.TradeId, sentTradeResult.TradeId);
        Assert.Equal(DEFAULT_RECEIVER_ID, sentTradeResult.ReceiverId);
    }

    [Fact(DisplayName = "Create a trade and return the received trade ids")]
    public async Task ListReceivedTradeIds_CreateSeveralTradesAndReturnTradeIdsList_ReturnsReceivedCreatedTradeIds()
    {
        // Arrange

        int count = 5;

        for (int i = 0; i < count; i++)
        {
            string tradeId = Guid.NewGuid().ToString();

            var tradeMock = new Trade
            {
                TradeId = tradeId,
                SentDate = DateTime.UtcNow
            };

            var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

            await _sut.AddSentAndReceivedTradeEntitiesAsync(tradeId, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);
        }

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var receivedTradesList = await _sut.ListReceivedTradeIdsAsync(DEFAULT_RECEIVER_ID);

        // Assert

        Assert.NotNull(receivedTradesList);
        Assert.Equal(count, receivedTradesList.Length);
    }

    [Fact(DisplayName = "Create a trade and return the received trade ids")]
    public async Task ListSentTradeIds_CreateSeveralTradesAndReturnTradeIdsList_ReturnsSentCreatedTradeIds()
    {
        // Arrange

        int count = 5;

        for (int i = 0; i < count; i++)
        {
            string tradeId = Guid.NewGuid().ToString();

            var tradeMock = new Trade
            {
                TradeId = tradeId,
                SentDate = DateTime.UtcNow
            };

            var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

            await _sut.AddSentAndReceivedTradeEntitiesAsync(tradeId, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);
        }

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var receivedTradesList = await _sut.ListSentTradeIdsAsync(DEFAULT_SENDER_ID);

        // Assert

        Assert.NotNull(receivedTradesList);
        Assert.Equal(count, receivedTradesList.Length);
    }

    [Fact(DisplayName = "Create a trade and return the received trade ids (cached)")]
    public async Task ListReceivedTradeIds_CreateSeveralTradesAndReturnTradeIdsList_ReturnsCachedReceivedCreatedTradeIds()
    {
        // Arrange

        int count = 5;

        for (int i = 0; i < count; i++)
        {
            string tradeId = Guid.NewGuid().ToString();

            var tradeMock = new Trade
            {
                TradeId = tradeId,
                SentDate = DateTime.UtcNow
            };

            var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

            await _sut.AddSentAndReceivedTradeEntitiesAsync(tradeId, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);
        }

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var receivedTradesList = await _sut.ListReceivedTradeIdsCachedAsync(DEFAULT_RECEIVER_ID);

        // Assert

        Assert.NotNull(receivedTradesList);
        Assert.Equal(count, receivedTradesList.Length);
    }

    [Fact(DisplayName = "Create a trade and return the received trade ids (cached)")]
    public async Task ListSentTradeIds_CreateSeveralTradesAndReturnTradeIdsList_ReturnsCachedSentCreatedTradeIds()
    {
        // Arrange

        int count = 5;

        for (int i = 0; i < count; i++)
        {
            string tradeId = Guid.NewGuid().ToString();

            var tradeMock = new Trade
            {
                TradeId = tradeId,
                SentDate = DateTime.UtcNow
            };

            var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

            await _sut.AddSentAndReceivedTradeEntitiesAsync(tradeId, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);
        }

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var receivedTradesList = await _sut.ListSentTradeIdsCachedAsync(DEFAULT_SENDER_ID);

        // Assert

        Assert.NotNull(receivedTradesList);
        Assert.Equal(count, receivedTradesList.Length);
    }
}
