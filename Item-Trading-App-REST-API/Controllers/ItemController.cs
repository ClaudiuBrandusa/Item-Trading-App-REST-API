using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Item;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Item;
using Item_Trading_App_REST_API.Hubs;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Item;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers
{
    [Authorize]
    public class ItemController : BaseController
    {
        private readonly IItemService _itemService;
        private readonly IHubContext<NotificationHub> _notificationHubContext;

        public ItemController(IItemService itemService, IHubContext<NotificationHub> notificationHubContext)
        {
            _itemService = itemService;
            _notificationHubContext = notificationHubContext;
        }

        [HttpGet(Endpoints.Item.Get)]
        public async Task<IActionResult> Get(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Item ID not provided" }
                });
            }

            var result = await _itemService.GetItemAsync(itemId);

            if (result == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            if(!result.Success)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = result.Errors
                });
            }

            return Ok(new ItemResponse
            {
                Id = result.ItemId,
                Name = result.ItemName,
                Description = result.ItemDescription
            });
        }

        [HttpGet(Endpoints.Item.List)]
        public async Task<IActionResult> List()
        {
            string searchString = HttpContext.Request.Query["searchstring"].ToString();

            var result = await _itemService.ListItems(searchString);

            if(result == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }    

            if(!result.Success)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = result.Errors
                });
            }

            return Ok(new ItemsResponse
            {
                ItemsId = result.ItemsId
            });
        }

        [HttpPost(Endpoints.Item.Create)]
        public async Task<IActionResult> Create([FromBody] CreateItemRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ItemName) || string.IsNullOrEmpty(request.ItemDescription))
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            var result = await _itemService.CreateItemAsync(new CreateItem { ItemName = request.ItemName, ItemDescription = request.ItemDescription });

            if(result == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            if(!result.Success)
            {
                return BadRequest(new CreateItemFailedResponse
                {
                    ItemName = request.ItemName,
                    Errors = result.Errors
                });
            }

            return Ok(new CreateItemSuccessResponse
            {
                ItemId = result.ItemId,
                ItemName = result.ItemName,
                ItemDescription = result.ItemDescription
            });
        }

        [HttpPatch(Endpoints.Item.Update)]
        public async Task<IActionResult> Update([FromBody] UpdateItemRequest request)
        {
            if(request == null || string.IsNullOrEmpty(request.ItemId) || string.IsNullOrEmpty(request.ItemName))
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            var result = await _itemService.UpdateItemAsync(new UpdateItem
            {
                ItemId = request.ItemId,
                ItemName = request.ItemName,
                ItemDescription = request.ItemDescription
            });

            if(result == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            if(!result.Success)
            {
                return BadRequest(new UpdateItemFailedResponse
                {
                    ItemId = result.ItemId,
                    ItemName = result.ItemName,
                    Errors = result.Errors
                });
            }

            return Ok(new UpdateItemSuccessResponse
            {
                ItemId = result.ItemId,
                ItemName = result.ItemName,
                ItemDescription = result.ItemDescription
            });
        }

        [HttpDelete(Endpoints.Item.Delete)]
        public async Task<IActionResult> Delete([FromBody] DeleteItemRequest request)
        {
            if(request == null || string.IsNullOrEmpty(request.ItemId))
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            var result = await _itemService.DeleteItemAsync(request.ItemId);

            if(result == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            if(!result.Success)
            {
                return BadRequest(new DeleteItemFailedResponse
                {
                    ItemId = result.ItemId,
                    ItemName = result.ItemName,
                    Errors = result.Errors
                });
            }

            return Ok(new DeleteItemSuccessResponse
            {
                ItemId = result.ItemId,
                ItemName = result.ItemName
            });
        }
    }
}
