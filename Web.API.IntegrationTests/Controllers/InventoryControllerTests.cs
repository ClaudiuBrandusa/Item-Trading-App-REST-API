using System.Security.Claims;
using CommonTestUtils.Extensions;
using CommonTestUtils.Wrappers;
using Domain.Entities.Items;
using Infrastructure.Data;
using Item_Trading_App_Contracts.Requests.Inventory;
using Item_Trading_App_Contracts.Responses.Inventory;
using Item_Trading_App_REST_API.Controllers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Web.API.IntegrationTests.Common.Factories;
using Web.API.IntegrationTests.Controllers.Common;
using static CommonTestUtils.Assertions.HttpResultAssert;
using static CommonTestUtils.Assertions.ResultPatternAssert;
using static CommonTestUtils.Utils.ControllerPackUtils;

namespace Web.API.IntegrationTests.Controllers;

public class InventoryControllerTests : IClassFixture<TestAppFactory>
{
    private const string DefaultFirstUserName = "Claudiu";
    private const string DefaultsSecondUserName = "Root";
    private readonly TestAppFactory _factory;

    public InventoryControllerTests(TestAppFactory factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task Add_WithValidRequest_ShouldAddTheItemToInventory()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var itemName = "Gold";
        var itemDescription = "itemDescription";

        (var user, var userClaims) = dbContext.GetUserWithClaimsByName(DefaultFirstUserName);

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var item = await Scenarios.CreateItem(
            _factory,
            itemName,
            itemDescription,
            TestContext.Current.CancellationToken
        );

        var itemId = item.ItemId;
        var quantity = 5; 
        
        var request = new AddItemRequest { ItemId = itemId, Quantity = quantity };

        var result = await controller.Add(request, TestContext.Current.CancellationToken);

        var response = AssertActionResultAsResponse<AddItemSuccessResponse>(result);
        Assert.NotNull(response);
        Assert.Equal(itemId, response.ItemId);
        Assert.Equal(itemName, response.ItemName);
        Assert.Equal(quantity, response.Quantity);
    }
    
    [Fact]
    public async Task Add_WithValidRequestTwice_ShouldAddTheItemToInventory()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var itemName = "Palladium";
        var itemDescription = "itemDescription";

        (var user, var userClaims) = dbContext.GetUserWithClaimsByName(DefaultFirstUserName);

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var item = await Scenarios.CreateItem(
            _factory,
            itemName,
            itemDescription,
            TestContext.Current.CancellationToken
        );

        var itemId = item.ItemId;
        var quantity = 5; 
        
        var request = new AddItemRequest { ItemId = itemId, Quantity = quantity };

        await controller.Add(request, TestContext.Current.CancellationToken);
        var result = await controller.Add(request, TestContext.Current.CancellationToken);

        var response = AssertActionResultAsResponse<AddItemSuccessResponse>(result);
        Assert.NotNull(response);
        Assert.Equal(itemId, response.ItemId);
        Assert.Equal(itemName, response.ItemName);
        Assert.Equal(quantity * 2, response.Quantity);
    }
    
    [Fact]
    public async Task Add_AddingAnItemThatDoesntExist_ShouldFail()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName(DefaultFirstUserName);

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemId = Item.GenerateId();
        var quantity = 5; 
        
        var request = new AddItemRequest { ItemId = itemId, Quantity = quantity };

        var result = await controller.Add(request, TestContext.Current.CancellationToken);

        var objectResult = AssertActionResponseBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultAsResponse<AddItemFailedResponse>(objectResult);
        AssertHasOnlyOneError(response);
    }

    [Fact]
    public async Task Drop_WithValidRequest_ShouldDropTheItemFromInventory()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        
        var itemName = "Silver";
        var itemDescription = "itemDescription";

        (var user, var userClaims) = dbContext.GetUserWithClaimsByName(DefaultFirstUserName);

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var createdItemResponse = await Scenarios.CreateItem(
            _factory,
            itemName,
            itemDescription,
            TestContext.Current.CancellationToken
        );

        var itemId = createdItemResponse.ItemId;
        var addedQuantity = 5;
        var droppedQuantity = 5;
        var remainedQuantity = addedQuantity - droppedQuantity;

        await Scenarios.AddItemToInventory(controller, itemId, addedQuantity, TestContext.Current.CancellationToken);

        var request = new DropItemRequest
        {
            ItemId = itemId,
            ItemQuantity = droppedQuantity
        };

        var result = await controller.Drop(request, TestContext.Current.CancellationToken);

        var response = AssertActionResultAsResponse<DropItemSuccessResponse>(result);
        Assert.NotNull(response);
        Assert.Equal(itemId, response.ItemId);
        Assert.Equal(itemName, response.ItemName);
        Assert.Equal(remainedQuantity, response.Quantity);
    }

    [Fact]
    public async Task Drop_DropItemThatDoesntExist_ShouldFail()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName(DefaultFirstUserName);

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemId = Item.GenerateId();
        var droppedQuantity = 5;

        var request = new DropItemRequest { ItemId = itemId, ItemQuantity = droppedQuantity };

        var result = await controller.Drop(request, TestContext.Current.CancellationToken);

        var objectResult = AssertActionResponseBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultAsResponse<DropItemFailedResponse>(objectResult);
        AssertHasOnlyOneError(response);
    }

    [Fact]
    public async Task Drop_ItemExistsButIsNotInInventory_ShouldFail()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        
        var itemName = "Granite";
        var itemDescription = "itemDescription";

        (var user, var userClaims) = dbContext.GetUserWithClaimsByName(DefaultFirstUserName);

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var createdItemResponse = await Scenarios.CreateItem(
            _factory,
            itemName,
            itemDescription,
            TestContext.Current.CancellationToken
        );

        var itemId = createdItemResponse.ItemId;
        var droppedQuantity = 5;

        var request = new DropItemRequest { ItemId = itemId, ItemQuantity = droppedQuantity };

        var result = await controller.Drop(request, TestContext.Current.CancellationToken);

        var objectResult = AssertActionResponseBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultAsResponse<DropItemFailedResponse>(objectResult);
        AssertHasOnlyOneError(response);
    }

    [Fact]
    public async Task Get_WithValidRequest_ReturnsItemFromInventory()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        
        var itemName = "Copper";
        var itemDescription = "itemDescription";

        (var user, var userClaims) = dbContext.GetUserWithClaimsByName(DefaultFirstUserName);

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var createdItemResponse = await Scenarios.CreateItem(
            _factory,
            itemName,
            itemDescription,
            TestContext.Current.CancellationToken
        );

        var itemId = createdItemResponse.ItemId;
        var addedQuantity = 5;

        await Scenarios.AddItemToInventory(controller, itemId, addedQuantity, TestContext.Current.CancellationToken);

        var result = await controller.Get(itemId, TestContext.Current.CancellationToken);

        var response = AssertActionResultAsResponse<GetItemSuccessResponse>(result);
        Assert.NotNull(response);
        Assert.Equal(itemId, response.ItemId);
        Assert.Equal(itemName, response.ItemName);
        Assert.Equal(itemDescription, response.ItemDescription);
        Assert.Equal(addedQuantity, response.Quantity);
    }
    
    [Fact]
    public async Task GetLockedAmount_WithValidRequest_ReturnsItemFromInventory()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        
        var itemName = "Tin";
        var itemDescription = "itemDescription";

        (var user, var userClaims) = dbContext.GetUserWithClaimsByName(DefaultFirstUserName);

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var createdItemResponse = await Scenarios.CreateItem(
            _factory,
            itemName,
            itemDescription,
            TestContext.Current.CancellationToken
        );

        var itemId = createdItemResponse.ItemId;
        var addedQuantity = 5;
        var lockedQuantity = 1;

        var mediator = controllerPack.ServiceScope.ServiceProvider.GetRequiredService<IMediator>();

        await Scenarios.AddItemToInventory(controller, itemId, addedQuantity, TestContext.Current.CancellationToken);
        await Scenarios.LockItemAmount(mediator, user!.Id, itemId, lockedQuantity, TestContext.Current.CancellationToken);

        var result = await controller.GetLockedAmount(itemId, TestContext.Current.CancellationToken);

        var response = AssertActionResultAsResponse<GetLockedAmountSuccessResponse>(result);
        Assert.NotNull(response);
        Assert.Equal(itemId, response.ItemId);
        Assert.Equal(itemName, response.ItemName);
        Assert.Equal(lockedQuantity, response.LockedAmount);
    }
    
    [Fact]
    public async Task GetLockedAmount_WhenNoLockedAmountWasSet_ShouldReturn0()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        
        var itemName = "Aluminum";
        var itemDescription = "itemDescription";

        (var user, var userClaims) = dbContext.GetUserWithClaimsByName(DefaultFirstUserName);

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var createdItemResponse = await Scenarios.CreateItem(
            _factory,
            itemName,
            itemDescription,
            TestContext.Current.CancellationToken
        );

        var itemId = createdItemResponse.ItemId;
        var addedQuantity = 5;
        
        await Scenarios.AddItemToInventory(controller, itemId, addedQuantity, TestContext.Current.CancellationToken);
        
        var result = await controller.GetLockedAmount(itemId, TestContext.Current.CancellationToken);

        var response = AssertActionResultAsResponse<GetLockedAmountSuccessResponse>(result);
        Assert.NotNull(response);
        Assert.Equal(itemId, response.ItemId);
        Assert.Equal(itemName, response.ItemName);
        Assert.Equal(0, response.LockedAmount);
    }
    
    [Fact]
    public async Task GetLockedAmount_WhenItemDoesntExist_ShouldReturn0()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName(DefaultFirstUserName);
        
        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemId = Item.GenerateId();
        
        var result = await controller.GetLockedAmount(itemId, TestContext.Current.CancellationToken);

        var objectResult = AssertActionResponseBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultAsResponse<GetLockedAmountFailedResponse>(objectResult);
        AssertHasOnlyOneError(response);
    }

    private ControllerPack<InventoryController, Program> CreateControllerPackWithUser(TestAppFactory factory, ClaimsPrincipal user)
    {
        return CreateControllerPackWithUser<InventoryController, Program>(factory, user);
    }
}