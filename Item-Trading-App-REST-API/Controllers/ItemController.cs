using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Item;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Item;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Commands.Item;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers;

[Authorize]
public class ItemController : BaseController
{
    private readonly IMediator _mediator;

    public ItemController(IMapper mapper, IMediator mediator) : base(mapper)
    {
        _mediator = mediator;
    }

    [HttpGet(Endpoints.Item.Get)]
    public async Task<IActionResult> Get(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return BadRequest(new FailedResponse
            {
                Errors = new[] { "Item ID not provided" }
            });

        var model = new GetItemQuery { ItemId = itemId };

        var result = await _mediator.Send(model);

        return MapResult<FullItemResult, ItemResponse, FailedResponse>(result);
    }

    [HttpGet(Endpoints.Item.List)]
    public async Task<IActionResult> List([FromQuery] string searchString)
    {
        var model = new ListItemsQuery { SearchString = searchString };

        var result = await _mediator.Send(model);

        return MapResult<ItemsResult, ItemsResponse, FailedResponse>(result);
    }
    
    [HttpGet(Endpoints.Item.ListTradesUsingTheItem)]
    public async Task<IActionResult> GetTradesUsingTheItem([FromQuery] string itemId)
    {
        var model = new GetTradesUsingTheItemQuery { ItemId = itemId };

        var results = await _mediator.Send(model);

        return Ok(new TradesUsingTheItemResponse { ItemId = itemId, TradeIds = results });
    }

    [HttpPost(Endpoints.Item.Create)]
    public async Task<IActionResult> Create([FromBody] CreateItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<CreateItemRequest, CreateItemCommand>(request, (nameof(CreateItemCommand.SenderUserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<FullItemResult, CreateItemSuccessResponse, CreateItemFailedResponse>(result);
    }

    [HttpPatch(Endpoints.Item.Update)]
    public async Task<IActionResult> Update([FromBody] UpdateItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<UpdateItemRequest, UpdateItemCommand>(request, (nameof(UpdateItemCommand.SenderUserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<FullItemResult, UpdateItemSuccessResponse, UpdateItemFailedResponse>(result);
    }

    [HttpDelete(Endpoints.Item.Delete)]
    public async Task<IActionResult> Delete([FromBody] DeleteItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(AdaptToType<ModelStateDictionary, FailedResponse>(ModelState));

        var model = AdaptToType<DeleteItemRequest, DeleteItemCommand>(request, (nameof(DeleteItemCommand.UserId), UserId));

        var result = await _mediator.Send(model);

        return MapResult<DeleteItemResult, DeleteItemSuccessResponse, DeleteItemFailedResponse>(result);
    }
}
