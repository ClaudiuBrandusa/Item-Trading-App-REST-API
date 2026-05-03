using CommonTestUtils.Wrappers;
using Domain.Entities.Items;
using Item_Trading_App_Contracts.Requests.Item;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Item;
using Item_Trading_App_REST_API.Controllers;
using Web.API.IntegrationTests.Common.Factories;
using Web.API.IntegrationTests.Controllers.Common;
using static CommonTestUtils.Assertions.HttpResultAssert;
using static CommonTestUtils.Assertions.ResultPatternAssert;
using static CommonTestUtils.Utils.ControllerPackUtils;

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
        using var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;

        var request = new CreateItemRequest
        {
            ItemName = "Test",
            ItemDescription = "Test"
        };

        var result = await controller.Create(request);

        var response = AssertActionResultAsResponse<CreateItemSuccessResponse>(result);
        Assert.NotNull(response);
        Assert.NotEmpty(response.ItemId);
        Assert.Equal(request.ItemName, response.ItemName);
        Assert.Equal(request.ItemDescription, response.ItemDescription);
    }

    [Fact]
    public async Task CreateItem_CreateItemWithInvalidData_ShouldFail()
    {
        using var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;

        var request = new CreateItemRequest
        {
            ItemName = string.Empty,
            ItemDescription = string.Empty
        };

        var result = await controller.Create(request);

        var objectResult = AssertActionResponseBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultAsResponse<CreateItemFailedResponse>(objectResult);
        AssertHasOnlyOneError(response);
    }

    [Fact]
    public async Task List_CreateServeralItemsThenListThemWithAnEmptySearchString_ShouldReturnAListOfItems()
    {
        using var controllerPack = CreateControllerPackWithDefaultUser(_factory);
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
            var response = AssertActionResultAsResponse<CreateItemSuccessResponse>(createdItemResult);
            createdItemResponses[index] = response!;
        });

        var searchString = string.Empty;

        var result = await controller.List(searchString);

        var itemsResponse = AssertActionResultAsResponse<ItemsResponse>(result);
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
        using var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;
        
        var itemName = "Brass";
        var itemDescription = "Test";

        var createdItemResponse = await Scenarios.CreateItem(
            controller,
            itemName,
            itemDescription
        );
        var itemId = createdItemResponse!.ItemId;

        var result = await controller.Get(itemId);

        var itemResponse = AssertActionResultAsResponse<ItemResponse>(result);
        Assert.NotNull(itemResponse);
        Assert.Equal(itemId, itemResponse.Id);
        Assert.Equal(itemName, itemResponse.Name);
        Assert.Equal(itemDescription, itemResponse.Description);
    }

    [Fact]
    public async Task GetItemById_WithNonExistingId_ShouldFail()
    {
        using var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;

        var itemId = Item.GenerateId();

        var result = await controller.Get(itemId);

        var objectResult = AssertActionResponseBadRequestObjectResult(result);
        var itemResponse = AssertBadRequestObjectResultAsResponse<FailedResponse>(objectResult);
        AssertHasOnlyOneError(itemResponse);
    }

    [Fact]
    public async Task UpdateItem_WithExistingId_ShouldUpdateTheItem()
    {
        using var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Lead";
        var itemDescription = "Test";

        var createdItemResponse = await Scenarios.CreateItem(
            controller,
            itemName,
            itemDescription
        );

        var itemId = createdItemResponse!.ItemId;

        var expectedUpdatedName = $"{itemName}_Updated";
        var expectedUpdatedDescription = $"{itemDescription}_Updated";

        var updateRequest = new UpdateItemRequest
        {
            ItemId = itemId,
            ItemName = expectedUpdatedName,
            ItemDescription = expectedUpdatedDescription
        };

        var result = await controller.Update(updateRequest);

        var updateItemResponse = AssertActionResultAsResponse<UpdateItemSuccessResponse>(result);
        Assert.NotNull(updateItemResponse);
        Assert.Equal(itemId, updateItemResponse.ItemId);
        Assert.Equal(expectedUpdatedName, updateItemResponse.ItemName);
        Assert.Equal(expectedUpdatedDescription, updateItemResponse.ItemDescription);
    }

    [Fact]
    public async Task UpdateItem_WithNonExistingId_ShouldFail()
    {
        using var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;

        var itemId = Item.GenerateId();

        var updateRequest = new UpdateItemRequest
        {
            ItemId = itemId,
            ItemName = "UpdatedName",
            ItemDescription = "UpdatedDescription"
        };

        var result = await controller.Update(updateRequest);

        var objectResult = AssertActionResponseBadRequestObjectResult(result);
        var updateItemResponse = AssertBadRequestObjectResultAsResponse<UpdateItemFailedResponse>(objectResult);
        AssertHasOnlyOneError(updateItemResponse);
    }

    [Fact]
    public async Task DeleteItem_WithExistingId_ShouldDeleteTheItem()
    {
        using var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;
        
        var itemName = "Brass";
        var itemDescription = "Test";

        var createdItemResponse = await Scenarios.CreateItem(
            controller,
            itemName,
            itemDescription
        );
        var itemId = createdItemResponse!.ItemId;

        var deleteItemRequest = new DeleteItemRequest
        {
            ItemId = itemId
        };

        var result = await controller.Delete(deleteItemRequest);

        var deleteItemResponse = AssertActionResultAsResponse<DeleteItemSuccessResponse>(result);
        Assert.NotNull(deleteItemResponse);
        Assert.Equal(itemId, deleteItemResponse.ItemId);
        Assert.Equal(itemName, deleteItemResponse.ItemName);
    }

    [Fact]
    public async Task DeleteItem_WithNonExistingId_ShouldFail()
    {
        using var controllerPack = CreateControllerPackWithDefaultUser(_factory);
        var controller = controllerPack.ControllerInstance;
        
        var itemId = Item.GenerateId();

        var deleteItemRequest = new DeleteItemRequest
        {
            ItemId = itemId
        };

        var result = await controller.Delete(deleteItemRequest);

        var objectResult = AssertActionResponseBadRequestObjectResult(result);
        var deleteItemResponse = AssertBadRequestObjectResultAsResponse<DeleteItemFailedResponse>(objectResult);
        AssertHasOnlyOneError(deleteItemResponse);
    }

    private ControllerPack<ItemController, Program>CreateControllerPackWithDefaultUser(TestAppFactory factory)
    {
        return CreateControllerPackWithDefaultUser<ItemController, Program>(factory);
    }
}
