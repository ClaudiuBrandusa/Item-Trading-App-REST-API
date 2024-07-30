using Domain.Entities.Items;
using Domain.Entities.Trades;
using Infrastructure.Services.DatabaseContextWrapper;
using Infrastructure_IntegrationTests.Utils;
using Infrastructure.Repositories.TradeItems;
using Domain.Repositories.TradeItemsHistory;

namespace Infrastructure_UnitTests;

public class TradeItemHistoryRepositoryTests
{
    private readonly ITradeItemHistoryRepository _sut;
    private readonly string DEFAULT_TRADE_ID = Guid.NewGuid().ToString();
    private readonly string DEFAULT_ITEM_ID = Guid.NewGuid().ToString();
    private const string DEFAULT_ITEM_NAME = "DefaultItemName";

    private IDatabaseContextWrapper _contextWrapper;

    public TradeItemHistoryRepositoryTests()
    {
        _contextWrapper = TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());
        var cacheServiceMock = TestingUtils.GetCacheServiceMock();
        var mapper = TestingUtils.GetMapper();

        _sut = new TradeItemHistoryRepository(_contextWrapper, mapper);
    }

    [Fact(DisplayName = "Add trade item history")]
    public async Task AddTradeItemHistory_AddNewTradeItemHistory_ReturnsTrue()
    {
        // Arrange

        int quantity = 5;
        int price = 5;

        var tradeItemMock = new TradeItem(DEFAULT_ITEM_ID, DEFAULT_ITEM_NAME, quantity, price);

        // Act

        var result = await _sut.AddTradeItemHistoryAsync(DEFAULT_TRADE_ID, DEFAULT_ITEM_NAME, tradeItemMock);

        // Assert

        Assert.True(result);
    }

    [Fact(DisplayName = "Add several trade items history then list them")]
    public async Task ListTradeContentHistory_AddNewTradeItemHistoryThenListTheTradeItemHistory_ReturnsTradeContentHistoryArray()
    {
        // Arrange

        int count = 5;
        int quantity = 5;
        int price = 5;

        for (int i = 0; i < count; i++)
        {
            var tradeItemMock = new TradeItem(Item.GenerateId(), $"{DEFAULT_ITEM_NAME}_{i}", quantity, price);
            
            var addTradeItemHistoryResult = await _sut.AddTradeItemHistoryAsync(DEFAULT_TRADE_ID, DEFAULT_ITEM_NAME, tradeItemMock);
        }

        // Act

        var listTradeItemsHistoryResult = await _sut.ListTradeItemsHistoryAsync(DEFAULT_TRADE_ID);

        // Assert

        Assert.NotNull(listTradeItemsHistoryResult);
        Assert.Equal(count, listTradeItemsHistoryResult.Length);
    }

    /*[Fact(DisplayName = "Add several trade items history then list them (cached)")]
    public async Task ListTradeContentHistory_AddNewTradeItemHistoryThenListTheTradeItemHistory_ReturnsCachedTradeContentHistoryArray()
    {
        // Arrange

        int count = 5;
        int quantity = 5;
        int price = 5;

        for (int i = 0; i < count; i++)
        {
            var tradeItemMock = new TradeItem(Item.GenerateId(), $"{DEFAULT_ITEM_NAME}_{i}", quantity, price);

            var addTradeItemHistoryResult = await _sut.AddTradeItemHistoryAsync(DEFAULT_TRADE_ID, tradeItemMock);
        }

        // Act

        var listTradeContentHistoryResult = await _sut.ListTradeContentHistoryAsTradeItemCachedAsync(DEFAULT_TRADE_ID);

        // Assert

        Assert.NotNull(listTradeContentHistoryResult);
        Assert.Equal(count, listTradeContentHistoryResult.Length);
    }*/

    [Fact(DisplayName = "Add trade item history then delete the trade item history")]
    public async Task DeleteTradeItemsHistoryForTrade_AddNewTradeItemHistoryThenDeleteTheAddedTradeItemHistory_ReturnsAmountOfDeletedEntities()
    {
        // Arrange

        int quantity = 5;
        int price = 5;

        var tradeItemMock = new TradeItem(DEFAULT_ITEM_ID, DEFAULT_ITEM_NAME, quantity, price);

        await _sut.AddTradeItemHistoryAsync(DEFAULT_TRADE_ID, DEFAULT_ITEM_NAME, tradeItemMock);

        // Act

        var result = await _sut.DeleteTradeItemsHistoryForTradeAsync(DEFAULT_TRADE_ID);

        // Assert

        Assert.Equal(1, result);
    }
}
