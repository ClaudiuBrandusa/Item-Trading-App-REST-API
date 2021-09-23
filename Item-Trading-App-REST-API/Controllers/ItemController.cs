using Item_Trading_App_Contracts;
using Item_Trading_App_Contracts.Requests.Item;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Item_Trading_App_REST_API.Controllers
{
    [Authorize]
    public class ItemController : BaseController
    {
        [HttpGet(Endpoints.Item.Get)]
        public IActionResult Get(string itemId)
        {
            return Ok();
        }

        [HttpGet(Endpoints.Item.List)]
        public IActionResult List()
        {
            return Ok();
        }

        [HttpPost(Endpoints.Item.Create)]
        public IActionResult Create([FromBody] CreateItemRequest request)
        {
            return Ok();
        }

        [HttpPut(Endpoints.Item.Add)]
        public IActionResult Add([FromBody] AddItemRequest request)
        {
            return Ok();
        }

        [HttpPatch(Endpoints.Item.Update)]
        public IActionResult Update([FromBody] UpdateItemRequest request)
        {
            return Ok();
        }

        [HttpDelete(Endpoints.Item.Delete)]
        public IActionResult Delete([FromBody] DeleteItemRequest request)
        {
            return Ok();
        }

        [HttpPost(Endpoints.Item.Drop)]
        public IActionResult Drop([FromBody] DropItemRequest request)
        {
            return Ok();
        }
    }
}
