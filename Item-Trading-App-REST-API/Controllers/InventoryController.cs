using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Inventory;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Controllers
{
    [Authorize]
    public class InventoryController : BaseController
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpPut(Endpoints.Inventory.Add)]
        public async Task<IActionResult> Add([FromBody] AddItemRequest request)
        {
            if (request == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            var result = await _inventoryService.AddItemAsync(UserId, request.ItemId, request.Quantity);

            if (result == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            if (!result.Success)
            {
                return BadRequest(new AddItemFailedResponse
                {
                    ItemId = request.ItemId,
                    ItemName = result.ItemName,
                    Quantity = request.Quantity,
                    Errors = result.Errors
                });
            }

            return Ok(new AddItemSuccessResponse
            {
                ItemId = result.ItemId,
                ItemName = result.ItemName,
                Quantity = result.Quantity
            });
        }

        [HttpPost(Endpoints.Inventory.Drop)]
        public async Task<IActionResult> Drop([FromBody] DropItemRequest request)
        {
            if (request == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            var result = await _inventoryService.DropItemAsync(UserId, request.ItemId, request.ItemQuantity);

            if (result == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            if (!result.Success)
            {
                return BadRequest(new DropItemFailedResponse
                {
                    ItemId = request.ItemId,
                    ItemName = result.ItemName,
                    Errors = result.Errors
                });
            }

            return Ok(new DropItemSuccessResponse
            {
                ItemId = result.ItemId,
                ItemName = result.ItemName,
                Quantity = result.Quantity
            });
        }

        [HttpGet(Endpoints.Inventory.Get)]
        public async Task<IActionResult> Get(string itemId)
        {
            if(string.IsNullOrEmpty(itemId))
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Item ID not provided" }
                });
            }

            var result = await _inventoryService.GetItemAsync(UserId, itemId);

            if(result == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            if(!result.Success)
            {
                return BadRequest(new GetItemFailedResponse
                {
                    ItemId = itemId,
                    Errors = result.Errors
                });
            }

            return Ok(new GetItemSuccessResponse
            {
                ItemId = result.ItemId,
                ItemName = result.ItemName,
                ItemDescription = result.ItemDescription,
                Quantity = result.Quantity
            });
        }

        [HttpGet(Endpoints.Inventory.List)]
        public async Task<IActionResult> List()
        {
            string searchString = HttpContext.Request.Query["searchstring"].ToString();

            var result = await _inventoryService.ListItemsAsync(UserId, searchString);

            if (result == null)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = new[] { "Something went wrong" }
                });
            }

            if (!result.Success)
            {
                return BadRequest(new FailedResponse
                {
                    Errors = result.Errors
                });
            }

            return Ok(new ListItemsSuccessResponse
            {
                ItemsId = result.ItemsId
            });
        }
    }
}
