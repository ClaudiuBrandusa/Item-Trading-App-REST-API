using CommonTestUtils.MockedServices;
using Domain.Aggregates.Trades;
using Domain.Entities.Identity;
using Domain.Repositories.Trades;
using Infrastructure.Repositories.Trades;
using Infrastructure.Services.DatabaseContextWrapper;
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
        _contextWrapper = DatabaseUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());
        var senderMock = new Mock<ISender>();

        _sut = new TradeRepository(_contextWrapper, senderMock.Object);
    }

    [Fact(DisplayName = "Create a trade and return the created trade")]
    public async Task GetTradeEntity_CreateTradeAndReturnTrade_ReturnsTheCreatedTrade()
    {
        // Arrange

        var tradeMock = new Trade(DateTime.UtcNow, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);

        await _sut.AddEntityAsync(tradeMock);

        // Act

        var tradeResult = await _sut.GetTradeEntityAsync(tradeMock.TradeId);

        // Assert

        Assert.NotNull(tradeResult);
        Assert.Equal(tradeMock, tradeResult);
    }

    [Fact(DisplayName = "Create a trade and return the received trade ids")]
    public async Task ListReceivedTradeIds_CreateSeveralTradesAndReturnTradeIdsList_ReturnsReceivedCreatedTradeIds()
    {
        // Arrange

        int count = 5;

        for (int i = 0; i < count; i++)
        {
            string tradeId = Trade.GenerateId();

            var tradeMock = new Trade(tradeId, DateTime.UtcNow, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);

            await _sut.AddEntityAsync(tradeMock);
        }

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync(CancellationToken.None);

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
            var tradeMock = new Trade(DateTime.UtcNow, DEFAULT_SENDER_ID, DEFAULT_RECEIVER_ID);

            await _sut.AddEntityAsync(tradeMock);
        }

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync(CancellationToken.None);

        // Act

        var receivedTradesList = await _sut.ListSentTradeIdsAsync(DEFAULT_SENDER_ID);

        // Assert

        Assert.NotNull(receivedTradesList);
        Assert.Equal(count, receivedTradesList.Length);
    }
}
