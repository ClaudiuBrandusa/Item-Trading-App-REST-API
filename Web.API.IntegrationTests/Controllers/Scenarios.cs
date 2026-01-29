using Item_Trading_App_Contracts.Responses.Item;
using Item_Trading_App_REST_API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Web.API.IntegrationTests.Controllers;

public static class Scenarios
{
    public static async Task CreateUser()
    {
        
    }

    public static async Task<CreateItemSuccessResponse> CreateItem(ItemController controller, string itemName, string itemDescription)
    {
        var request = new Item_Trading_App_Contracts.Requests.Item.CreateItemRequest
        {
            ItemName = itemName,
            ItemDescription = itemDescription
        };

        var createdItemResult = await controller.Create(request);

        var createdItemObjectResult = createdItemResult as OkObjectResult;
        return (createdItemObjectResult!.Value! as CreateItemSuccessResponse)!;
    }

    public static async Task AddItemToInventory()
    {
        
    }

    public static async Task CreateTrade()
    {
        
    }
}