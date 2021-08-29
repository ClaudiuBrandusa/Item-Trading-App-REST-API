using Item_Trading_App_Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Item_Trading_App_REST_API.Controllers
{
    [Authorize]
    public class ItemController : Controller
    {
        [HttpGet(Endpoints.Item.Get)]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpGet(Endpoints.Item.List)]
        public IActionResult List()
        {
            return Ok();
        }

        [HttpPost(Endpoints.Item.Create)]
        public IActionResult Create()
        {
            return Ok();
        }

        [HttpPut(Endpoints.Item.Add)]
        public IActionResult Add()
        {
            return Ok();
        }

        [HttpPatch(Endpoints.Item.Update)]
        public IActionResult Update()
        {
            return Ok();
        }

        [HttpDelete(Endpoints.Item.Delete)]
        public IActionResult Delete()
        {
            return Ok();
        }
    }
}
