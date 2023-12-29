using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Inventory;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Inventory;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers;

[Authorize]
public class InventoryController : BaseController
{
    private readonly IMediator _mediator;

    public InventoryController(IMapper mapper, IMediator mediator) : base(mapper)
    {
        _mediator = mediator;
    }

    [HttpPut(Endpoints.Inventory.Add)]
    public async Task<IActionResult> Add([FromBody] AddItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<AddItemRequest, AddInventoryItemCommand>(request, (nameof(AddInventoryItemCommand.UserId), UserId), (nameof(AddInventoryItemCommand.Notify), true));

        var result = await _mediator.Send(model);

        return MapResult<QuantifiedItemResult, AddItemSuccessResponse, AddItemFailedResponse>(result);
    }

    [HttpPost(Endpoints.Inventory.Drop)]
    public async Task<IActionResult> Drop([FromBody] DropItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<DropItemRequest, DropInventoryItemCommand>(request, (nameof(DropInventoryItemCommand.UserId), UserId), (nameof(DropInventoryItemCommand.Notify), true));

        var result = await _mediator.Send(model);

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

        var model = AdaptToType<string, GetInventoryItemQuery>(itemId, (nameof(GetInventoryItemQuery.UserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<QuantifiedItemResult, GetItemSuccessResponse, GetItemFailedResponse>(result);
    }

    [HttpGet(Endpoints.Inventory.List)]
    public async Task<IActionResult> List([FromQuery] string searchString)
    {
        var model = AdaptToType<string, ListInventoryItemsQuery>(searchString, (nameof(ListInventoryItemsQuery.UserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<ItemsResult, ListItemsSuccessResponse, FailedResponse>(result);
    }

    [HttpGet(Endpoints.Inventory.GetLockedAmount)]
    public async Task<IActionResult> GetLockedAmount(string itemId)
    {
        var model = AdaptToType<string, GetInventoryItemLockedAmountQuery>(itemId, (nameof(GetInventoryItemLockedAmountQuery.UserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<LockedItemAmountResult, GetLockedAmountSuccessResponse, GetLockedAmountFailedResponse>(result);
    }
}
