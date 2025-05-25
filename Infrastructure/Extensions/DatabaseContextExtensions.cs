using Application.Services.UnitOfWork;
using Domain.Aggregates.Inventory;
using Domain.Aggregates.Trades;
using Domain.Entities.Identity;
using Domain.Entities.Inventory;
using Domain.Entities.Items;
using Domain.Entities.Trades;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class DatabaseContextExtensions
{
    private const string ROOT_USERNAME = "root";
    private const string ROOT_PASSWORD = "!Ab12345";
    private const string SECONDARY_USERNAME = "Claudiu";
    private const string SECONDARY_PASSWORD = "!Ab12345";

    public static async Task SeedDatabase(this DatabaseContext databaseContext, IServiceProvider serviceProvider)
    {
        if (databaseContext.Users.Any()) return;

        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

        var usersDataCollection = new (string username, string password)[]
        {
            (ROOT_USERNAME, ROOT_PASSWORD),
            (SECONDARY_USERNAME, SECONDARY_PASSWORD)
        };

        var unitOfWorkService = serviceProvider.GetRequiredService<IUnitOfWorkService>();

        await unitOfWorkService.ExplicitTransaction(async () =>
        {
            try
            {
                // add the items

                var items = await GenerateAndSeedItemData(databaseContext);

                foreach (var (username, password) in usersDataCollection)
                {
                    var user = await CreateAndSeedUser(userManager, username, password);

                    // add inventory items

                    await SeedUserInventoryItems(databaseContext, items, user.Id);
                }

                // add trades

                var userIds = databaseContext.Users.Select(x => x.Id).ToArray();

                var itemsToBeAdded = new string[] { items[0].ItemId, items[1].ItemId, items[2].ItemId };

                await SeedTrades(databaseContext, userIds[0], userIds[1], items.Select(x => x.ItemId).ToArray());

                var trades = databaseContext.Trades.ToArray();

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to seed the database");
                return false;
            }
        });
    }

    private static async Task<Item[]> GenerateAndSeedItemData(DatabaseContext databaseContext)
    {
        var itemsData = new (string itemName, string itemDescription)[]
            {
                ("Gold", "This is a resource"),
                ("Bronze", "This is an alloy"),
                ("Wood", "This is a resource"),
                ("Iron", "This is a resource"),
                ("Copper", "This is a resource")
            };

        var items = new Item[itemsData.Length];

        for (var i = 0; i < itemsData.Length; i++)
        {
            var item = new Item(itemsData[i].itemName, itemsData[i].itemDescription);

            items[i] = item;

            await databaseContext.Items.AddAsync(item);
        }

        await databaseContext.SaveChangesAsync();

        return items;
    }

    public static async Task<User> CreateAndSeedUser(UserManager<User> userManager, string username, string password)
    {
        static string formatEmail(string id) => $"{id}@item_trading.app";

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            Email = formatEmail(username)
        };

        user.UpdateCashAmount(500);

        var createUserResult = await userManager.CreateAsync(user, password);

        return user;
    }

    private static async Task SeedUserInventoryItems(DatabaseContext databaseContext, Item[] items, string userId)
    {
        var random = new Random();

        for (int i = 0; i < items.Length; i++)
        {
            int itemQuantity = random.Next(15, 50);

            var ownedItem = new OwnedItem(items[i].ItemId, userId, itemQuantity);

            await databaseContext.OwnedItems.AddAsync(ownedItem);
        }

        await databaseContext.SaveChangesAsync();
    }

    private static async Task SeedTrades(DatabaseContext databaseContext, string firstUserId, string secondUserId, string[] availableItemIds)
    {
        var random = new Random();

        int tradesCount = random.Next(3, 7);

        for (int i = 0; i < tradesCount; i++)
        {
            string senderId;
            string receiverId;

            if (random.Next(0, 2) == 0)
            {
                senderId = firstUserId;
                receiverId = secondUserId;
            }
            else
            {
                senderId = secondUserId;
                receiverId = firstUserId;
            }

            await SeedTradeWithRandomItems(databaseContext, random, firstUserId, secondUserId, availableItemIds);
        }
    }

    private static Task<Trade> SeedTradeWithRandomItems(DatabaseContext databaseContext, Random random, string senderUserId, string receiverUserId, string[] availableItemIds)
    {
        int itemsCount = random.Next(1, availableItemIds.Length);

        var itemsToBeAdded = availableItemIds.Take(itemsCount).ToArray();

        return SeedTrade(databaseContext, senderUserId, receiverUserId, itemsToBeAdded);
    }

    private static async Task<Trade> SeedTrade(DatabaseContext databaseContext, string senderUserId, string receiverUserId, string[] itemsToBeAdded)
    {
        var trade = new Trade(DateTime.Now);

        trade.SetSender(senderUserId);
        trade.SetReceiver(receiverUserId);

        AddTradeContents(itemsToBeAdded, trade);
        await AddLockedItemsForTradeContent(databaseContext, trade);

        await databaseContext.Trades.AddAsync(trade);
        await databaseContext.SaveChangesAsync();

        return trade;
    }

    private static void AddTradeContents(string[] itemsToBeAdded, Trade trade)
    {
        for (int i = 0; i < itemsToBeAdded.Length; i++)
        {
            var tradeContent = CreateTradeContentWithRandomData(itemsToBeAdded[i], trade.TradeId);

            trade.AddTradeContent(tradeContent);
        }
    }

    private static async Task AddLockedItemsForTradeContent(DatabaseContext databaseContext, Trade trade)
    {
        foreach(var tradeItem in trade.TradeContents)
        {
            LockedItem lockedItem = databaseContext.LockedItems.FirstOrDefault(x => x.UserId == trade.SentTrade.SenderId && x.ItemId == tradeItem.ItemId);

            if (lockedItem is null)
            {
                lockedItem = new LockedItem(trade.SentTrade.SenderId, tradeItem.ItemId, tradeItem.Quantity);
                await databaseContext.LockedItems.AddAsync(lockedItem);
            }
            else
            {
                lockedItem.AddLockedAmount(tradeItem.Quantity);
            }
        }
    }

    private static TradeItem CreateTradeContentWithRandomData(string itemId, string tradeId)
    {
        var random = new Random();

        return new TradeItem(tradeId, itemId, random.Next(2, 5), random.Next(1, 5));
    }
}
 