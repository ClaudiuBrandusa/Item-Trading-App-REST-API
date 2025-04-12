using Domain.Aggregates.Inventory;
using Domain.Aggregates.Trades;
using Domain.Entities.Identity;
using Domain.Entities.Items;
using Infrastructure.IntegrationTests.Common;
using Infrastructure.Repositories.Identity;
using Infrastructure.Repositories.Inventory;
using Infrastructure.Repositories.Items;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.IntegrationTests.Utils;

public static class TestingScenarios
{
    public static async Task<User> CreateUser(IServiceProvider serviceProvider, string userName)
    {
        var dbContextWrapper = GetDatabaseContextWrapper(serviceProvider);
        var userManager = GetUserManager(serviceProvider);
        var repository = new IdentityRepository(dbContextWrapper, userManager);

        var userToBeCreated = new User
        {
            Id = User.GenerateId(),
            UserName = userName,
            Email = $"{userName}@email.com"
        };

        await repository.CreateUserAsync(userToBeCreated, Constants.DEFAULT_USER_PASSWORD);

        var response = await repository.GetUserByIdAsync(userToBeCreated.Id);

        return response!;
    }

    public static async Task<User> CreateUser(IServiceProvider serviceProvider, string userName, string email)
    {
        var dbContextWrapper = GetDatabaseContextWrapper(serviceProvider);
        var userManager = GetUserManager(serviceProvider);
        var repository = new IdentityRepository(dbContextWrapper, userManager);

        var userToBeCreated = new User
        {
            Id = User.GenerateId(),
            UserName = userName,
            Email = email
        };

        await repository.CreateUserAsync(userToBeCreated, Constants.DEFAULT_USER_PASSWORD);

        var response = await repository.GetUserByIdAsync(userToBeCreated.Id);

        return response!;
    }

    public static async Task<User> CreateUser(IServiceProvider serviceProvider, string userName, string email, string password)
    {
        var dbContextWrapper = GetDatabaseContextWrapper(serviceProvider);
        var userManager = GetUserManager(serviceProvider);
        var repository = new IdentityRepository(dbContextWrapper, userManager);

        var userToBeCreated = new User
        {
            Id = User.GenerateId(),
            UserName = userName,
            Email = email
        };

        await repository.CreateUserAsync(userToBeCreated, password);

        var response = await repository.GetUserByIdAsync(userToBeCreated.Id);

        return response!;
    }

    public static async Task<(User,User)> CreateSenderReceiverUsersPair(IServiceProvider serviceProvider, int index)
    {
        var senderUser = await CreateUser(serviceProvider, $"sender_{index}");
        var receiverUser = await CreateUser(serviceProvider, $"receiver_{index}");

        return (senderUser, receiverUser);
    }

    public static async Task<Item> CreateItem(IServiceProvider serviceProvider, string itemName, string itemDescription)
    {
        var dbContextWrapper = GetDatabaseContextWrapper(serviceProvider);
        var userManager = GetUserManager(serviceProvider);
        var repository = new ItemRepository(dbContextWrapper);

        var itemToBeCreated = new Item(itemName, itemDescription);

        await repository.AddEntityAsync(itemToBeCreated);

        var response = await repository.GetItemEntityAsync(itemToBeCreated.ItemId);

        return response!;
    }

    public static async Task<OwnedItem> AddItemToUser(IServiceProvider serviceProvider, Item item, User user, int quantity)
    {
        var dbContextWrapper = GetDatabaseContextWrapper(serviceProvider);
        var userManager = GetUserManager(serviceProvider);
        var repository = new InventoryRepository(dbContextWrapper, TestingUtils.GetMapper());

        var ownedItem = new OwnedItem(item.ItemId, user.Id, quantity);

        await repository.AddEntityAsync(ownedItem);

        var response = await repository.GetOwnedItemEntityAsync(user.Id, item.ItemId);

        return response!;
    }

    public static Trade CreateTrade(string senderUserId, string receiverUserId)
    {
        DateTime sentDate = DateTime.Now;
        return CreateTrade(sentDate, senderUserId, receiverUserId);
    }

    public static Trade CreateTrade(DateTime sentDate, string senderUserId, string receiverUserId)
    {
        var trade = new Trade(sentDate);
        trade.SetSender(senderUserId);
        trade.SetReceiver(receiverUserId);

        return trade;
    }

    private static IDatabaseContextWrapper GetDatabaseContextWrapper(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IDatabaseContextWrapper>();
    }

    private static UserManager<User> GetUserManager(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<UserManager<User>>();
    }
}
