using Domain.Aggregates.Trades;
using Domain.Entities.Identity;
using Domain.Repositories.Trades;
using Infrastructure.Repositories.Trades;
using Infrastructure.Services.DatabaseContextWrapper;
using Infrastructure_IntegrationTests.Utils;
using MediatR;
using Moq;

namespace Infrastructure_UnitTests.RepositoryTests;

public class TradeRepositoryTests
{
    private readonly ITradeRepository _sut;
    private readonly string DEFAULT_SENDER_ID = User.GenerateId();
    private readonly string DEFAULT_RECEIVER_ID = User.GenerateId();

    private IDatabaseContextWrapper _contextWrapper;

    public TradeRepositoryTests()
    {
        _contextWrapper = TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());
        var senderMock = new Mock<ISender>();

        _sut = new TradeRepository(_contextWrapper, senderMock.Object);
    }

    [Fact(DisplayName = "Create a trade and return the created trade")]
    public async Task GetTradeEntity_CreateTradeAndReturnTrade_ReturnsTheCreatedTrade()
    {
        // Arrange

        var tradeMock = new Trade(DateTime.UtcNow);

        var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

        // Act

        var tradeResult = await _sut.GetTradeEntityAsync(tradeMock.TradeId);

        // Assert

        Assert.NotNull(tradeResult);
        Assert.Equal(tradeMock, tradeResult);
    }

    /*[Fact(DisplayName = "Create a trade and return the created trade (cached)")]
    public async Task GetCachedTrade_CreateTradeAndReturnTrade_ReturnsTheCachedCreatedTrade()
    {
        // Arrange

        var tradeMock = new Trade(DateTime.UtcNow);

        var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

        await _sut.AddSentAndReceivedTradeEntitiesAsync(tradeMock.TradeId, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var cachedTradeResult = await _sut.GetCachedTradeAsync(tradeMock.TradeId);
        
        // Assert

        Assert.NotNull(cachedTradeResult);
        Assert.True(cachedTradeResult.IsPartOfTrade(tradeMock), "The cached trade should belong to the same trade as the trade mock");
        Assert.Equal(DEFAULT_SENDER_ID, cachedTradeResult.SenderUserId);
        Assert.Equal(DEFAULT_RECEIVER_ID, cachedTradeResult.ReceiverUserId);
    }*/

    [Fact(DisplayName = "Create a trade and return the sent trade")]
    public async Task GetSentTrade_CreateTradeAndReturnTrade_ReturnsTheSentCreatedTrade()
    {
        // Arrange

        var tradeMock = new Trade(DateTime.UtcNow);

        var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

        await _sut.AddSentAndReceivedTradeEntitiesAsync(tradeMock.TradeId, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);

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

        var tradeMock = new Trade(DateTime.UtcNow);

        var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

        await _sut.AddSentAndReceivedTradeEntitiesAsync(tradeMock.TradeId, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);

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
            string tradeId = Trade.GenerateId();

            var tradeMock = new Trade(tradeId, DateTime.UtcNow);

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
            var tradeMock = new Trade(DateTime.UtcNow);

            var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

            await _sut.AddSentAndReceivedTradeEntitiesAsync(tradeMock.TradeId, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);
        }

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var receivedTradesList = await _sut.ListSentTradeIdsAsync(DEFAULT_SENDER_ID);

        // Assert

        Assert.NotNull(receivedTradesList);
        Assert.Equal(count, receivedTradesList.Length);
    }

    /*[Fact(DisplayName = "Create a trade and return the received trade ids (cached)")]
    public async Task ListReceivedTradeIds_CreateSeveralTradesAndReturnTradeIdsList_ReturnsCachedReceivedCreatedTradeIds()
    {
        // Arrange

        int count = 5;

        for (int i = 0; i < count; i++)
        {
            var tradeMock = new Trade(DateTime.UtcNow);

            var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

            await _sut.AddSentAndReceivedTradeEntitiesAsync(tradeMock.TradeId, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);
        }

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var receivedTradesList = await _sut.ListReceivedTradeIdsCachedAsync(DEFAULT_RECEIVER_ID);

        // Assert

        Assert.NotNull(receivedTradesList);
        Assert.Equal(count, receivedTradesList.Length);
    }*/

    /*[Fact(DisplayName = "Create a trade and return the received trade ids (cached)")]
    public async Task ListSentTradeIds_CreateSeveralTradesAndReturnTradeIdsList_ReturnsCachedSentCreatedTradeIds()
    {
        // Arrange

        int count = 5;

        for (int i = 0; i < count; i++)
        {
            var tradeMock = new Trade(DateTime.UtcNow);

            var createdTradeResult = await _sut.AddEntityAsync(tradeMock);

            await _sut.AddSentAndReceivedTradeEntitiesAsync(tradeMock.TradeId, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);
        }

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var receivedTradesList = await _sut.ListSentTradeIdsCachedAsync(DEFAULT_SENDER_ID);

        // Assert

        Assert.NotNull(receivedTradesList);
        Assert.Equal(count, receivedTradesList.Length);
    }*/
}
