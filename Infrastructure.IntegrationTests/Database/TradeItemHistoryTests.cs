using Domain.Repositories.TradeItemsHistory;
using Infrastructure.IntegrationTests.Common;
using Infrastructure.IntegrationTests.Utils;
using Infrastructure.Repositories.TradeItems;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.IntegrationTests.Database;
public class TradeItemHistoryTests : IClassFixture<DatabaseFixture>
{
    private readonly ITradeItemHistoryRepository _repository;
    private readonly IServiceProvider _serviceProvider;

    public TradeItemHistoryTests(DatabaseFixture fixture)
    {
        var dbContextWrapper = fixture.ServiceProvider.GetRequiredService<IDatabaseContextWrapper>();
        _repository = new TradeItemHistoryRepository(dbContextWrapper, TestingUtils.GetMapper());
        _serviceProvider = fixture.ServiceProvider;
    }

    [Fact(DisplayName = "Add trade item history")]
    public async Task AddTradeItemHistory_AddTradeItemHistoryThenReturnTheEntity_ReturnsTheCreatedTradeItemHistory()
    {
        // Arrange

        const int expectedItemAmount = 5;
        const int expectedPrice = 10;

        (var senderUser, var receiverUser) = await TestingScenarios.CreateSenderReceiverUsersPair(_serviceProvider, 0);
        var trade = TestingScenarios.CreateTrade(senderUser.Id, receiverUser.Id);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "item", string.Empty);
        var inventoryItem = await TestingScenarios.AddItemToUser(_serviceProvider, item, senderUser, expectedItemAmount);
        var tradeItem = TestingScenarios.CreateTradeItem(trade.TradeId, item.ItemId, expectedItemAmount, expectedPrice);

        trade.AddTradeContent(tradeItem);

        await _repository.AddEntityAsync(trade);

        // Act

        var result = await _repository.AddTradeItemHistoryAsync(trade.TradeId, item.Name, tradeItem);
        var tradeItemHistory = await _repository.GetTradeItemHistoryAsync(tradeItem.ItemId);

        // Assert

        Assert.True(result);
        Assert.NotNull(tradeItemHistory);
        Assert.Equal(tradeItem.ItemId, tradeItemHistory.ItemId);
        Assert.Equal(item.Name, tradeItemHistory.ItemName);
        Assert.Equal(trade.TradeId, tradeItemHistory.TradeId);
        Assert.Equal(expectedItemAmount, tradeItemHistory.Quantity);
        Assert.Equal(expectedPrice, tradeItemHistory.Price);
    }

    // Add and delete trade item history
    [Fact(DisplayName = "Add trade item history, list the trade items history for the given trade then remove the trade items history for that trade and then list the remained trade items history (no trade item history should remain)")]
    public async Task DeleteTradeItemsHistoryForTrade_CreateTradeItemHistoryForTradeThenRemoveThem_ReturnsNoTradeItemsHistoryForThisTrade()
    {
        // Arrange

        const int expectedItemAmount = 5;
        const int expectedPrice = 10;

        (var senderUser, var receiverUser) = await TestingScenarios.CreateSenderReceiverUsersPair(_serviceProvider, 1);
        var trade = TestingScenarios.CreateTrade(senderUser.Id, receiverUser.Id);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "item", string.Empty);
        var inventoryItem = await TestingScenarios.AddItemToUser(_serviceProvider, item, senderUser, expectedItemAmount);
        var tradeItem = TestingScenarios.CreateTradeItem(trade.TradeId, item.ItemId, expectedItemAmount, expectedPrice);

        trade.AddTradeContent(tradeItem);

        await _repository.AddEntityAsync(trade);

        // Act

        var addTradeItemHistoryResult = await _repository.AddTradeItemHistoryAsync(trade.TradeId, item.Name, tradeItem);
        var tradeItemsHistoryIds = await _repository.ListTradeItemsHistoryAsync(trade.TradeId);
        var deleteTradeItemsHistoryForTradeResult = await _repository.DeleteTradeItemsHistoryForTradeAsync(trade.TradeId);
        var remainedTradeItemsHistoryIds = await _repository.ListTradeItemsHistoryAsync(trade.TradeId);

        // Assert

        Assert.True(addTradeItemHistoryResult);
        Assert.Single(tradeItemsHistoryIds);
        Assert.Equal(tradeItem.ItemId, tradeItemsHistoryIds[0].ItemId);
        Assert.Equal(1, deleteTradeItemsHistoryForTradeResult);
        Assert.Empty(remainedTradeItemsHistoryIds);
    }
}
