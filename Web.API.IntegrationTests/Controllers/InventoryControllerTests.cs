using System.Security.Claims;
using Domain.Entities.Identity;
using Domain.Entities.Items;
using Infrastructure.Data;
using Item_Trading_App_Contracts.Requests.Inventory;
using Item_Trading_App_Contracts.Responses.Inventory;
using Item_Trading_App_REST_API.Controllers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Web.API.IntegrationTests.Common.Factories;
using Web.API.IntegrationTests.Controllers.Common;
using static Web.API.IntegrationTests.Controllers.Common.Utils;

namespace Web.API.IntegrationTests.Controllers;

public class InventoryControllerTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public InventoryControllerTests(TestAppFactory factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task Add_WithValidRequest_ShouldAddTheItemToInventory()
    {
        var itemName = "Gold";
        var itemDescription = "itemDescription";

        var dbContextFactory = _factory.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        var dbContext = dbContextFactory.CreateDbContext();
        var user = dbContext.Users.FirstOrDefault(x => x.UserName == "Claudiu");

        var userClaims = CreateClaimsFromUser((user as User)!);

        var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var item = await Scenarios.CreateItem(
            CreateControllerPackWithDefaultUser<ItemController>(_factory).ControllerInstance,
            itemName,
            itemDescription
        );

        var itemId = item.ItemId;
        var quantity = 5; 
        
        var request = new AddItemRequest { ItemId = itemId, Quantity = quantity };

        var result = await controller.Add(request);

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<AddItemSuccessResponse>(objectResult);
        
        Assert.NotNull(response);
        Assert.Equal(itemId, response.ItemId);
        Assert.Equal(itemName, response.ItemName);
        Assert.Equal(quantity, response.Quantity);
    }
    
    [Fact]
    public async Task Add_AddingAnItemThatDoesntExist_ShouldFail()
    {
        var dbContextFactory = _factory.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        var dbContext = dbContextFactory.CreateDbContext();
        var user = dbContext.Users.FirstOrDefault(x => x.UserName == "Claudiu");

        var userClaims = CreateClaimsFromUser((user as User)!);

        var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemId = Item.GenerateId();
        var quantity = 5; 
        
        var request = new AddItemRequest { ItemId = itemId, Quantity = quantity };

        var result = await controller.Add(request);

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<AddItemFailedResponse>(objectResult);
        AssertResponseHasOnlyOneError(response);
    }

    [Fact]
    public async Task Drop_WithValidRequest_ShouldDropTheItemFromInventory()
    {
        var itemName = "Silver";
        var itemDescription = "itemDescription";

        var dbContextFactory = _factory.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        var dbContext = dbContextFactory.CreateDbContext();
        var user = dbContext.Users.FirstOrDefault(x => x.UserName == "Claudiu");

        var userClaims = CreateClaimsFromUser((user as User)!);

        var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var createdItemResponse = await Scenarios.CreateItem(
            CreateControllerPackWithDefaultUser<ItemController>(_factory).ControllerInstance,
            itemName,
            itemDescription
        );

        var itemId = createdItemResponse.ItemId;
        var addedQuantity = 5;
        var droppedQuantity = 5;
        var remainedQuantity = addedQuantity - droppedQuantity;

        var addedInventoryItemResponse = await Scenarios.AddItemToInventory(controller, itemId, addedQuantity);

        var request = new DropItemRequest
        {
            ItemId = itemId,
            ItemQuantity = droppedQuantity
        };

        var result = await controller.Drop(request);

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<DropItemSuccessResponse>(objectResult);
        
        Assert.NotNull(response);
        Assert.Equal(itemId, response.ItemId);
        Assert.Equal(itemName, response.ItemName);
        Assert.Equal(remainedQuantity, response.Quantity);
    }

    [Fact]
    public async Task Drop_DropItemThatDoesntExist_ShouldFail()
    {
        var dbContextFactory = _factory.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        var dbContext = dbContextFactory.CreateDbContext();
        var user = dbContext.Users.FirstOrDefault(x => x.UserName == "Claudiu");

        var userClaims = CreateClaimsFromUser((user as User)!);

        var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemId = Item.GenerateId();
        var droppedQuantity = 5;

        var request = new DropItemRequest { ItemId = itemId, ItemQuantity = droppedQuantity };

        var result = await controller.Drop(request);

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<DropItemFailedResponse>(objectResult);
        AssertResponseHasOnlyOneError(response);
    }

    [Fact]
    public async Task Drop_ItemExistsButIsNotInInventory_ShouldFail()
    {
        var itemName = "Granite";
        var itemDescription = "itemDescription";

        var dbContextFactory = _factory.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        var dbContext = dbContextFactory.CreateDbContext();
        var user = dbContext.Users.FirstOrDefault(x => x.UserName == "Claudiu");

        var userClaims = CreateClaimsFromUser((user as User)!);

        var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var createdItemResponse = await Scenarios.CreateItem(
            CreateControllerPackWithDefaultUser<ItemController>(_factory).ControllerInstance,
            itemName,
            itemDescription
        );

        var itemId = createdItemResponse.ItemId;
        var droppedQuantity = 5;

        var request = new DropItemRequest { ItemId = itemId, ItemQuantity = droppedQuantity };

        var result = await controller.Drop(request);

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<DropItemFailedResponse>(objectResult);
        AssertResponseHasOnlyOneError(response);
    }

    [Fact]
    public async Task Get_WithValidRequest_ReturnsItemFromInventory()
    {
        var itemName = "Copper";
        var itemDescription = "itemDescription";

        var dbContextFactory = _factory.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        var dbContext = dbContextFactory.CreateDbContext();
        var user = dbContext.Users.FirstOrDefault(x => x.UserName == "Claudiu");

        var userClaims = CreateClaimsFromUser((user as User)!);

        var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var createdItemResponse = await Scenarios.CreateItem(
            CreateControllerPackWithDefaultUser<ItemController>(_factory).ControllerInstance,
            itemName,
            itemDescription
        );

        var itemId = createdItemResponse.ItemId;
        var addedQuantity = 5;

        var addedInventoryItemResponse = await Scenarios.AddItemToInventory(controller, itemId, addedQuantity);

        var result = await controller.Get(itemId);

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<GetItemSuccessResponse>(objectResult);
        
        Assert.NotNull(response);
        Assert.Equal(itemId, response.ItemId);
        Assert.Equal(itemName, response.ItemName);
        Assert.Equal(itemDescription, response.ItemDescription);
        Assert.Equal(addedQuantity, response.Quantity);
    }
    
    [Fact]
    public async Task GetLockedAmount_WithValidRequest_ReturnsItemFromInventory()
    {
        var itemName = "Tin";
        var itemDescription = "itemDescription";

        var dbContextFactory = _factory.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        var dbContext = dbContextFactory.CreateDbContext();
        var user = dbContext.Users.FirstOrDefault(x => x.UserName == "Claudiu");

        var userClaims = CreateClaimsFromUser((user as User)!);

        var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var createdItemResponse = await Scenarios.CreateItem(
            CreateControllerPackWithDefaultUser<ItemController>(_factory).ControllerInstance,
            itemName,
            itemDescription
        );

        var itemId = createdItemResponse.ItemId;
        var addedQuantity = 5;
        var lockedQuantity = 1;

        var mediator = controllerPack.ServiceScope.ServiceProvider.GetRequiredService<IMediator>();

        var addedInventoryItemResponse = await Scenarios.AddItemToInventory(controller, itemId, addedQuantity);
        var lockItemAmountResult = await Scenarios.LockItemAmount(mediator, user!.Id, itemId, lockedQuantity);

        var result = await controller.GetLockedAmount(itemId);

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<GetLockedAmountSuccessResponse>(objectResult);
        
        Assert.NotNull(response);
        Assert.Equal(itemId, response.ItemId);
        Assert.Equal(itemName, response.ItemName);
        Assert.Equal(lockedQuantity, response.LockedAmount);
    }
    
    [Fact]
    public async Task GetLockedAmount_WhenNoLockedAmountWasSet_ShouldReturn0()
    {
        var itemName = "Aluminum";
        var itemDescription = "itemDescription";

        var dbContextFactory = _factory.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        var dbContext = dbContextFactory.CreateDbContext();
        var user = dbContext.Users.FirstOrDefault(x => x.UserName == "Claudiu");

        var userClaims = CreateClaimsFromUser((user as User)!);

        var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var createdItemResponse = await Scenarios.CreateItem(
            CreateControllerPackWithDefaultUser<ItemController>(_factory).ControllerInstance,
            itemName,
            itemDescription
        );

        var itemId = createdItemResponse.ItemId;
        var addedQuantity = 5;
        
        var mediator = controllerPack.ServiceScope.ServiceProvider.GetRequiredService<IMediator>();

        var addedInventoryItemResponse = await Scenarios.AddItemToInventory(controller, itemId, addedQuantity);
        
        var result = await controller.GetLockedAmount(itemId);

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<GetLockedAmountSuccessResponse>(objectResult);
        
        Assert.NotNull(response);
        Assert.Equal(itemId, response.ItemId);
        Assert.Equal(itemName, response.ItemName);
        Assert.Equal(0, response.LockedAmount);
    }
    
    [Fact]
    public async Task GetLockedAmount_WhenItemDoesntExist_ShouldReturn0()
    {
        var dbContextFactory = _factory.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        var dbContext = dbContextFactory.CreateDbContext();
        var user = dbContext.Users.FirstOrDefault(x => x.UserName == "Claudiu");

        var userClaims = CreateClaimsFromUser((user as User)!);

        var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemId = Item.GenerateId();
        
        var result = await controller.GetLockedAmount(itemId);

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<GetLockedAmountFailedResponse>(objectResult);
        AssertResponseHasOnlyOneError(response);
    }

    private ControllerPack<InventoryController> CreateControllerPackWithDefaultUser(TestAppFactory factory)
    {
        return CreateControllerPackWithDefaultUser<InventoryController>(factory);
    }

    private ControllerPack<InventoryController> CreateControllerPackWithUser(TestAppFactory factory, ClaimsPrincipal user)
    {
        return CreateControllerPackWithUser<InventoryController>(factory, user);
    }

    private ControllerPack<T> CreateControllerPackWithUser<T>(TestAppFactory factory, ClaimsPrincipal user) where T : BaseController
    {
        var controllerPack = new ControllerPack<T>(factory);

        controllerPack.SetUser(user);

        return controllerPack;
    }

    private ControllerPack<T> CreateControllerPackWithDefaultUser<T>(TestAppFactory factory) where T : BaseController
    {
        var controllerPack = new ControllerPack<T>(factory);

        var user = CreateDefaultUser();

        controllerPack.SetUser(user);

        return controllerPack;
    }
}