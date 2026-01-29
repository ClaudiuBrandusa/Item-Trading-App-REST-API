using Item_Trading_App_Contracts.Requests.Item;
using Item_Trading_App_Contracts.Responses.Item;
using Item_Trading_App_REST_API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Web.API.IntegrationTests.Common.Factories;

namespace Web.API.IntegrationTests.Controllers;

public class ItemControllerTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public ItemControllerTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateItem_CreatesANewItem_ShouldCreateTheItemSuccessfully()
    {
        var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;

        var request = new Item_Trading_App_Contracts.Requests.Item.CreateItemRequest
        {
            ItemName = "Test",
            ItemDescription = "Test"
        };

        var result = await controller.Create(request);

        var objectResult = Utils.AssertActionResultAsOkObjectResult(result);
        var response = Utils.AssertOkObjectResultSuccessResponse<CreateItemSuccessResponse>(objectResult);
        Assert.NotNull(response);
        Assert.NotEmpty(response.ItemId);
        Assert.Equal(request.ItemName, response.ItemName);
        Assert.Equal(request.ItemDescription, response.ItemDescription);
    }

    [Fact]
    public async Task List_CreateServeralItemsThenListThemWithAnEmptySearchString_ShouldReturnAListOfItems()
    {
        var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;

        var expectedItemAmount = 10;
        var baseItemName = "ItemName";
        var baseItemDescription = "ItemDescription";
        var createItemRequests = new CreateItemRequest[expectedItemAmount];

        for (int i = 0; i < expectedItemAmount; i++)
        {
            var itemName = $"{baseItemName}_{i}";
            var itemDescription = $"{baseItemDescription}_{i}";

            createItemRequests[i] = new CreateItemRequest
            {
                ItemName = itemName,
                ItemDescription = itemDescription
            };
        }

        var createdItemResponses = new CreateItemSuccessResponse[expectedItemAmount];

        await Parallel.ForAsync(0, expectedItemAmount, async (index, ct) =>
        {
            var createdItemResult = await controller.Create(createItemRequests[index]);
            var createdItemObjectResult = createdItemResult as OkObjectResult;
            var response = createdItemObjectResult!.Value as CreateItemSuccessResponse;
            createdItemResponses[index] = response;
        });

        var searchString = string.Empty;

        var result = await controller.List(searchString);

        var objectResult = Utils.AssertActionResultAsOkObjectResult(result);
        var itemsResponse = Utils.AssertOkObjectResultSuccessResponse<ItemsResponse>(objectResult);
        
        var createdItemIds = createdItemResponses.Select(x => x.ItemId).ToArray();

        Assert.NotNull(itemsResponse);
        Assert.NotNull(itemsResponse.ItemsId);
        var itemIds = itemsResponse.ItemsId.ToArray();
        Assert.True(itemIds.Length >= expectedItemAmount);
        Assert.All(createdItemIds, (itemId) =>
        {
            Assert.Contains(itemId, itemIds);
        });
    }

    [Fact]
    public async Task GetItemById_WithExistingId_ShouldReturnTheItem()
    {
        var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;

        var request = new Item_Trading_App_Contracts.Requests.Item.CreateItemRequest
        {
            ItemName = "Test",
            ItemDescription = "Test"
        };

        var createdItemResult = await controller.Create(request);
        var createdItemObjectResult = createdItemResult as OkObjectResult;
        var createItemResponse = createdItemObjectResult!.Value as CreateItemSuccessResponse;
        var itemId = createItemResponse!.ItemId;

        var result = await controller.Get(itemId);

        var objectResult = Utils.AssertActionResultAsOkObjectResult(result);
        var itemResponse = Utils.AssertOkObjectResultSuccessResponse<ItemResponse>(objectResult);
        Assert.NotNull(itemResponse);
        Assert.Equal(itemId, itemResponse.Id);
        Assert.Equal(request.ItemName, itemResponse.Name);
        Assert.Equal(request.ItemDescription, itemResponse.Description);
    }

    [Fact]
    public async Task UpdateItem_WithExistingId_ShouldUpdateTheItem()
    {
        var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;

        var request = new Item_Trading_App_Contracts.Requests.Item.CreateItemRequest
        {
            ItemName = "Test",
            ItemDescription = "Test"
        };

        var createdItemResult = await controller.Create(request);
        var createdItemObjectResult = createdItemResult as OkObjectResult;
        var createItemResponse = createdItemObjectResult!.Value as CreateItemSuccessResponse;
        var itemId = createItemResponse!.ItemId;

        var expectedUpdatedName = $"{request.ItemName}_Updated";
        var expectedUpdatedDescription = $"{request.ItemDescription}_Updated";

        var updateRequest = new UpdateItemRequest
        {
            ItemId = itemId,
            ItemName = expectedUpdatedName,
            ItemDescription = expectedUpdatedDescription
        };

        var result = await controller.Update(updateRequest);

        var objectResult = Utils.AssertActionResultAsOkObjectResult(result);
        var updateItemResponse = Utils.AssertOkObjectResultSuccessResponse<UpdateItemSuccessResponse>(objectResult);
        Assert.NotNull(updateItemResponse);
        Assert.Equal(itemId, updateItemResponse.ItemId);
        Assert.Equal(expectedUpdatedName, updateItemResponse.ItemName);
        Assert.Equal(expectedUpdatedDescription, updateItemResponse.ItemDescription);
    }

    [Fact]
    public async Task DeleteItem_WithExistingId_ShouldDeleteTheItem()
    {
        var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;

        var request = new Item_Trading_App_Contracts.Requests.Item.CreateItemRequest
        {
            ItemName = "Test",
            ItemDescription = "Test"
        };

        var createdItemResult = await controller.Create(request);
        var createdItemObjectResult = createdItemResult as OkObjectResult;
        var createItemResponse = createdItemObjectResult!.Value as CreateItemSuccessResponse;
        var itemId = createItemResponse!.ItemId;

        var deleteItemRequest = new DeleteItemRequest
        {
            ItemId = itemId
        };

        var result = await controller.Delete(deleteItemRequest);

        var objectResult = Utils.AssertActionResultAsOkObjectResult(result);
        var deleteItemResponse = Utils.AssertOkObjectResultSuccessResponse<DeleteItemSuccessResponse>(objectResult);
        
        Assert.NotNull(deleteItemResponse);
        Assert.Equal(itemId, deleteItemResponse.ItemId);
        Assert.Equal(request.ItemName, deleteItemResponse.ItemName);
    }

    private ControllerPack<ItemController> CreateControllerPackWithDefaultUser(TestAppFactory factory)
    {
        var controllerPack = new ControllerPack<ItemController>(factory);

        var user = Utils.CreateDefaultUser();

        controllerPack.SetUser(user);

        return controllerPack;
    }
}
