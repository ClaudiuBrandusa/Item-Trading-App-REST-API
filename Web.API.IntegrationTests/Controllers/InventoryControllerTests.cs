using System.Security.Claims;
using Domain.Entities.Identity;
using Infrastructure.Data;
using Item_Trading_App_Contracts.Requests.Inventory;
using Item_Trading_App_Contracts.Responses.Inventory;
using Item_Trading_App_REST_API.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Web.API.IntegrationTests.Common.Factories;

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
        var itemName = "itemName";
        var itemDescription = "itemDescription";

        var dbContextFactory = _factory.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();//await dbContextProvider.ProvideDatabaseContextAsync();
        var dbContext = dbContextFactory.CreateDbContext();
        var user = dbContext.Users.FirstOrDefault(x => x.UserName == "Claudiu");

        var userClaims = Utils.CreateClaimsFromUser((user as User)!);

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

        var objectResult = Utils.AssertActionResultAsOkObjectResult(result);
        var response = Utils.AssertOkObjectResultSuccessResponse<AddItemSuccessResponse>(objectResult);
        
        Assert.NotNull(response);
        Assert.Equal(itemId, response.ItemId);
        Assert.Equal(quantity, response.Quantity);
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

        var user = Utils.CreateDefaultUser();

        controllerPack.SetUser(user);

        return controllerPack;
    }
}