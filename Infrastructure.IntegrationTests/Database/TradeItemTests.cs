using Domain.Aggregates.Inventories;
using Domain.Entities.Items;
using Domain.Entities.Trades;
using Domain.Repositories.TradeItems;
using Infrastructure.IntegrationTests.Common.Fixtures;
using Infrastructure.IntegrationTests.Utils;
using Infrastructure.Repositories.TradeItems;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.IntegrationTests.Database;
public class TradeItemTests : IClassFixture<DatabaseFixture>
{
    private readonly ITradeItemRepository _repository;
    private readonly IServiceProvider _serviceProvider;

    public TradeItemTests(DatabaseFixture databaseFixture)
    {
        var dbContextWrapper = databaseFixture.ServiceProvider.GetRequiredService<IDatabaseContextWrapper>();
        _repository = new TradeContentRepository(dbContextWrapper);
        _serviceProvider = databaseFixture.ServiceProvider;
    }

    [Fact(DisplayName = "Create trade item then return it")]
    public async Task AddTradeItem_CreateTradeItemAndReturnIt_ReturnsCreatedItem()
    {
        // Arrange

        const int price = 10;
        const int quantity = 5;

        (var senderUser, var receiverUser) = await TestingScenarios.CreateSenderReceiverUsersPair(_serviceProvider, 0);
        var trade = TestingScenarios.CreateTrade(senderUser.Id, receiverUser.Id);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Gold", string.Empty);
        var inventoryItem = await TestingScenarios.AddItemToUser(_serviceProvider, item, senderUser, quantity);
        var tradeContent = new TradeItem(trade.TradeId, inventoryItem.ItemId, inventoryItem.Quantity, price);

        trade.AddTradeContent(tradeContent);

        // Act

        var tradeCreated = await _repository.AddEntityAsync(trade);

        var retrievedTradeItem = await _repository.GetTradeItemAsync(trade.TradeId, item.ItemId);

        // Assert

        Assert.True(tradeCreated);
        Assert.NotNull(retrievedTradeItem);
        Assert.Equal(inventoryItem.ItemId, retrievedTradeItem.ItemId);
        Assert.Equal(inventoryItem.Quantity, retrievedTradeItem.Quantity);
        Assert.Equal(price, retrievedTradeItem.Price);
    }

    [Fact(DisplayName = "Create trade item update it then return it")]
    public async Task UpdateTradeItem_CreateTradeItemAndUpdateItThenReturnIt_ReturnsUpdatedItem()
    {
        // Arrange

        const int initialPrice = 10;
        const int initialQuantity = 5;
        const int updatedPrice = initialPrice + 5;
        const int updatedQuantity = initialQuantity + 10;

        (var senderUser, var receiverUser) = await TestingScenarios.CreateSenderReceiverUsersPair(_serviceProvider, 1);
        var trade = TestingScenarios.CreateTrade(senderUser.Id, receiverUser.Id);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Silver", string.Empty);
        var inventoryItem = await TestingScenarios.AddItemToUser(_serviceProvider, item, senderUser, initialQuantity);
        var tradeContent = new TradeItem(trade.TradeId, inventoryItem.ItemId, inventoryItem.Quantity, initialPrice);

        trade.AddTradeContent(tradeContent);

        // Act

        var tradeCreated = await _repository.AddEntityAsync(trade);

        tradeContent.UpdateQuantity(updatedQuantity);
        tradeContent.UpdatePrice(updatedPrice);

        var tradeItemUpdated = await _repository.UpdateEntityAsync(tradeContent);

        var retrievedTradeItem = await _repository.GetTradeItemAsync(trade.TradeId, item.ItemId);

        // Assert

        Assert.NotNull(retrievedTradeItem);
        Assert.True(tradeCreated);
        Assert.True(tradeItemUpdated);
        Assert.Equal(inventoryItem.ItemId, retrievedTradeItem.ItemId);
        Assert.Equal(trade.TradeId, retrievedTradeItem.TradeId);
        Assert.Equal(updatedPrice, retrievedTradeItem.Price);
        Assert.Equal(updatedQuantity, retrievedTradeItem.Quantity);
    }

    /*[Fact(DisplayName = "Create few trade items and list them then check their ids")]
    public async Task ListTradeItems_CreateFewTradeItemsThenListThem_ReturnsListOfTradeItemIds()
    {
        // Arrange

        var dbFixture = await DatabaseFixture.BuildDatabaseFixture();
        var serviceProvider = dbFixture.ServiceProvider;
        var dbContextWrapper = serviceProvider.GetRequiredService<IDatabaseContextWrapper>();
        var repository = new TradeContentRepository(dbContextWrapper);

        const int expectedTradeItemsAmount = 3;
        const int expectedItemAmount = 10;
        const int expectedPrice = 5;

        (var senderUser, var receiverUser) = await TestingScenarios.CreateSenderReceiverUsersPair(serviceProvider, 2);
        var trade = TestingScenarios.CreateTrade(senderUser.Id, receiverUser.Id);

        var itemNames = new string[] { "Aluminum", "Cobalt", "Zinc" };
        var items = new Item[expectedTradeItemsAmount];
        var inventoryItems = new OwnedItem[expectedTradeItemsAmount];
        var tradeContents = new TradeItem[expectedTradeItemsAmount];

        for (int i = 0; i < expectedTradeItemsAmount; i++)
        {
            items[i] = await TestingScenarios.CreateItemAsync(serviceProvider, itemNames[i], string.Empty);
            var inventoryItem = await TestingScenarios.AddItemToUser(serviceProvider, items[i], senderUser, expectedItemAmount);
            inventoryItems[i] = inventoryItem;
            var tradeContent = TestingScenarios.CreateTradeItem(trade.TradeId, inventoryItem.ItemId, expectedItemAmount, expectedPrice);
            tradeContents[i] = tradeContent;
            trade.AddTradeContent(tradeContents[i]);
        }

        // Act

        var tradeCreated = await repository.AddEntityAsync(trade);
        var tradeItemIds = await repository.ListTradeItemsAsync(trade.TradeId);

        // Assert

        Assert.NotNull(tradeItemIds);
        Assert.Equal(expectedTradeItemsAmount, tradeItemIds.Length);
        Assert.All(tradeItemIds, x => tradeContents.Any(y => x.ItemId == y.ItemId));
    }*/

    [Fact(DisplayName = "Create trade item and delete it then check if it was deleted properly")]
    public async Task DeleteTradeItem_CreateTradeItemAndDeleteTradeItemThenCheckIfItWasDeletedProperly_ShouldntFindTheItem()
    {
        // Arrange

        const int price = 10;
        const int quantity = 5;

        (var senderUser, var receiverUser) = await TestingScenarios.CreateSenderReceiverUsersPair(_serviceProvider, 3);
        var trade = TestingScenarios.CreateTrade(senderUser.Id, receiverUser.Id);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Iron", string.Empty);
        var inventoryItem = await TestingScenarios.AddItemToUser(_serviceProvider, item, senderUser, quantity);
        var tradeContent = TestingScenarios.CreateTradeItem(trade.TradeId, inventoryItem.ItemId, inventoryItem.Quantity, price);

        trade.AddTradeContent(tradeContent);

        // Act

        var tradeCreated = await _repository.AddEntityAsync(trade);

        await _repository.DeleteTradeItemsAsync(trade.TradeId);

        var retrievedTradeItem = await _repository.GetTradeItemAsync(trade.TradeId, item.ItemId);

        // Assert

        Assert.True(tradeCreated);
        Assert.Null(retrievedTradeItem);
    }

    [Fact(DisplayName = "Create trades using two items (each trade has one of the two available items) and get the trade ids by the used trade item")]
    public async Task GetTradeIdsUsingItemGetTradeIdsUsingItem_CreateSeveralTradesUsingTheSameItem_ReturnsTradeIdsUsingTheSameItem()
    {
        // Arrange

        const int price = 10;
        const int quantity = 5;
        const int tradesUsingFirstItemAmount = 3;
        const int tradesUsingSecondItemAmount = 2;

        (var senderUser, var receiverUser) = await TestingScenarios.CreateSenderReceiverUsersPair(_serviceProvider, 4);
        
        var firstItem = await TestingScenarios.CreateItemAsync(_serviceProvider, "Tin", string.Empty);
        var firstInventoryItem = await TestingScenarios.AddItemToUser(_serviceProvider, firstItem, senderUser, quantity * tradesUsingFirstItemAmount);
        var secondItem = await TestingScenarios.CreateItemAsync(_serviceProvider, "Copper", string.Empty);
        var secondInventoryItem = await TestingScenarios.AddItemToUser(_serviceProvider, secondItem, senderUser, quantity * tradesUsingSecondItemAmount);
        var tradeIdsUsingFirstItem = new string[tradesUsingFirstItemAmount];
        var tradeIdsUsingSecondItem = new string[tradesUsingSecondItemAmount];

        for (int i = 0; i < tradesUsingFirstItemAmount; i++)
        {
            var trade = TestingScenarios.CreateTrade(senderUser.Id, receiverUser.Id);

            var tradeContent = TestingScenarios.CreateTradeItem(trade.TradeId, firstInventoryItem.ItemId, firstInventoryItem.Quantity, price);

            trade.AddTradeContent(tradeContent);

            await _repository.AddEntityAsync(trade);

            tradeIdsUsingFirstItem[i] = trade.TradeId;
        }

        for (int i = 0; i < tradesUsingSecondItemAmount; i++)
        {
            var trade = TestingScenarios.CreateTrade(senderUser.Id, receiverUser.Id);

            var tradeContent = new TradeItem(trade.TradeId, secondInventoryItem.ItemId, secondInventoryItem.Quantity, price);

            trade.AddTradeContent(tradeContent);

            await _repository.AddEntityAsync(trade);

            tradeIdsUsingSecondItem[i] = trade.TradeId;
        }

        await _repository.SaveChangesAsync();

        // Act

        var tradeIdsUsingTheFirstItem = await _repository.GetTradeIdsUsingItemAsync(firstInventoryItem.ItemId);
        var tradeIdsUsingTheSecondItem = await _repository.GetTradeIdsUsingItemAsync(secondInventoryItem.ItemId);

        // Assert

        Assert.NotNull(tradeIdsUsingTheFirstItem);
        Assert.NotNull(tradeIdsUsingTheSecondItem);
        Assert.Equal(tradesUsingFirstItemAmount, tradeIdsUsingTheFirstItem.Length);
        Assert.Equal(tradesUsingSecondItemAmount, tradeIdsUsingTheSecondItem.Length);
        Assert.All(tradeIdsUsingTheFirstItem, x => tradeIdsUsingFirstItem.Any(y => x == y));
        Assert.All(tradeIdsUsingTheSecondItem, x => tradeIdsUsingSecondItem.Any(y => x == y));
    }
}
