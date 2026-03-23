using Application.Behaviors.Trade.CreateTrade;
using Application.Models.TradeItems;
using Application.Services.Trades;
using Application.Services.UnitOfWork;
using Domain.Aggregates.Inventories;
using Domain.Aggregates.Trades;
using Domain.Entities.Identity;
using Domain.Entities.Items;
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
        var tradeService = serviceProvider.GetRequiredService<ITradeService>();

        await unitOfWorkService.ExplicitTransaction(async () =>
        {
            try
            {
                // add the items

                var items = await GenerateAndSeedItemData(databaseContext);

                foreach (var (username, password) in usersDataCollection)
                {
                    var user = await CreateAndSeedUser(userManager, username, password);

                    var inventory = new Inventory(user.Id);

                    // add inventory items

                    SeedUserInventory(inventory, items);

                    await databaseContext.Inventories.AddAsync(inventory);

                    await databaseContext.SaveChangesAsync();
                }

                // add trades

                var userIds = databaseContext.Users.Select(x => x.Id).ToArray();

                var itemsToBeAdded = new string[] { items[0].ItemId, items[1].ItemId, items[2].ItemId };

                await SeedTrades(tradeService, userIds[0], userIds[1], items.Select(x => (x.ItemId, x.Name)).ToArray());

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

    private static void SeedUserInventory(Inventory inventory, Item[] items)
    {
        var random = new Random();

        for (int i = 0; i < items.Length; i++)
        {
            int itemQuantity = random.Next(15, 50);

            inventory.AddItem(items[i].ItemId, itemQuantity);
        }
    }

    private static async Task SeedTrades(ITradeService tradeService, string firstUserId, string secondUserId, (string itemId, string itemName)[] availableItemData)
    {
        var random = new Random();

        int tradesCount = random.Next(3, 7);

        // two flags are declared in order to ensure that there was at least one trade sent from each user
        bool firstUserSentAtLeastOneTrade = false;
        bool secondUserSentAtLeastOneTrade = false;

        for (int i = 0; i < tradesCount; i++)
        {
            string senderId;
            string receiverId;

            if (random.Next(0, 2) == 0)
            {
                senderId = firstUserId;
                receiverId = secondUserId;
                firstUserSentAtLeastOneTrade = true;
            }
            else
            {
                senderId = secondUserId;
                receiverId = firstUserId;
                secondUserSentAtLeastOneTrade = true;
            }

            await SeedTradeWithRandomItems(tradeService, random, senderId, receiverId, availableItemData);
        }

        if (!firstUserSentAtLeastOneTrade)
        {
            await SeedTradeWithRandomItems(tradeService, random, firstUserId, secondUserId, availableItemData);
        }
        else if (!secondUserSentAtLeastOneTrade)
        {
            await SeedTradeWithRandomItems(tradeService, random, secondUserId, firstUserId, availableItemData);
        }
    }

    private static Task<Trade> SeedTradeWithRandomItems(ITradeService tradeService, Random random, string senderUserId, string receiverUserId, (string itemId, string itemName)[] availableItemData)
    {
        int itemsCount = random.Next(1, availableItemData.Length);

        var itemsToBeAdded = availableItemData.Take(itemsCount).ToArray();

        return SeedTrade(tradeService, senderUserId, receiverUserId, itemsToBeAdded);
    }

    private static async Task<Trade> SeedTrade(ITradeService tradeService, string senderUserId, string receiverUserId, (string itemId, string itemName)[] itemDataToBeAdded)
    {
        var trade = new Trade(DateTime.UtcNow, senderUserId, receiverUserId);

        var tradeItems = new TradeItemDTO[itemDataToBeAdded.Length];

        for (int i = 0; i < itemDataToBeAdded.Length; i++)
        {
            tradeItems[i] = CreateTradeItemWithRandomData(itemDataToBeAdded[i].itemId, itemDataToBeAdded[i].itemName);
        }

        var createTradeCommand = new CreateTradeOfferCommand
        {
            SenderUserId = senderUserId,
            TargetUserId = receiverUserId,
            Items = tradeItems
        };

        await tradeService.CreateTradeOfferAsync(createTradeCommand);

        return trade;
    }

    private static TradeItemDTO CreateTradeItemWithRandomData(string itemId, string itemName)
    {
        var random = new Random();

        return new TradeItemDTO
        {
            ItemId = itemId,
            ItemName = itemName,
            Quantity = random.Next(2, 5),
            Price = random.Next(1, 5)
        };
    }
}
