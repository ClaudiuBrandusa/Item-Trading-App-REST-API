using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Inventory;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Inventory;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Inventory;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers;

[Authorize]
public class InventoryController : BaseController
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService, IMapper mapper) : base(mapper)
    {
        _inventoryService = inventoryService;
    }

    [HttpPut(Endpoints.Inventory.Add)]
    public async Task<IActionResult> Add([FromBody] AddItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var result = await _inventoryService.AddItemAsync(AdaptToType<AddItemRequest, AddItem>(request, (nameof(AddItem.UserId), UserId)));

        return MapResult<QuantifiedItemResult, AddItemSuccessResponse, AddItemFailedResponse>(result);
    }

    [HttpPost(Endpoints.Inventory.Drop)]
    public async Task<IActionResult> Drop([FromBody] DropItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var result = await _inventoryService.DropItemAsync(AdaptToType<DropItemRequest, DropItem>(request, (nameof(DropItem.UserId), UserId)));

        return MapResult<QuantifiedItemResult, AddItemSuccessResponse, AddItemFailedResponse>(result);
    }

    [HttpGet(Endpoints.Inventory.Get)]
    public async Task<IActionResult> Get(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return BadRequest(new FailedResponse
            {
                Errors = new[] { "Item ID was not provided" }
            });

        var result = await _inventoryService.GetItemAsync(AdaptToType<string, GetUsersItem>(itemId, (nameof(GetUsersItem.UserId), UserId)));

        return MapResult<QuantifiedItemResult, GetItemSuccessResponse, GetItemFailedResponse>(result);
    }

    [HttpGet(Endpoints.Inventory.List)]
    public async Task<IActionResult> List()
    {
        string searchString = HttpContext.Request.Query["searchstring"].ToString();

        var result = await _inventoryService.ListItemsAsync(AdaptToType<string , ListItems>(searchString, (nameof(ListItems.UserId), UserId)));

        return MapResult<ItemsResult, ListItemsSuccessResponse, FailedResponse>(result);
    }

    [HttpGet(Endpoints.Inventory.GetLockedAmount)]
    public async Task<IActionResult> GetLockedAmount(string itemId)
    {
        var result = await _inventoryService.GetLockedAmount(AdaptToType<string, GetUsersItem>(itemId, (nameof(GetUsersItem.UserId), UserId)));

        return MapResult<LockedItemAmountResult, GetLockedAmountSuccessResponse, GetLockedAmountFailedResponse>(result);
    }
}
