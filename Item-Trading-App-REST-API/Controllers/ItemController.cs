using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Item;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Item;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Item;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers;

[Authorize]
public class ItemController : BaseController
{
    private readonly IItemService _itemService;

    public ItemController(IItemService itemService, IMapper mapper) : base(mapper)
    {
        _itemService = itemService;
    }

    [HttpGet(Endpoints.Item.Get)]
    public async Task<IActionResult> Get(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return BadRequest(new FailedResponse
            {
                Errors = new[] { "Item ID not provided" }
            });

        var result = await _itemService.GetItemAsync(itemId);

        return MapResult<FullItemResult, ItemResponse, FailedResponse>(result);
    }

    [HttpGet(Endpoints.Item.List)]
    public async Task<IActionResult> List()
    {
        string searchString = HttpContext.Request.Query["searchstring"].ToString();

        var result = await _itemService.ListItemsAsync(searchString);

        return MapResult<ItemsResult, ItemsResponse, FailedResponse>(result);
    }

    [HttpPost(Endpoints.Item.Create)]
    public async Task<IActionResult> Create([FromBody] CreateItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<CreateItemRequest, CreateItem>(request, (nameof(CreateItem.SenderUserId), UserId));

        var result = await _itemService.CreateItemAsync(model);

        return MapResult<FullItemResult, CreateItemSuccessResponse, CreateItemFailedResponse>(result);
    }

    [HttpPatch(Endpoints.Item.Update)]
    public async Task<IActionResult> Update([FromBody] UpdateItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<UpdateItemRequest, UpdateItem>(request, (nameof(UpdateItem.SenderUserId), UserId));

        var result = await _itemService.UpdateItemAsync(model);

        return MapResult<FullItemResult, UpdateItemSuccessResponse, UpdateItemFailedResponse>(result);
    }

    [HttpDelete(Endpoints.Item.Delete)]
    public async Task<IActionResult> Delete([FromBody] DeleteItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var result = await _itemService.DeleteItemAsync(request.ItemId, UserId);

        return MapResult<DeleteItemResult, DeleteItemSuccessResponse, DeleteItemFailedResponse>(result);
    }
}
