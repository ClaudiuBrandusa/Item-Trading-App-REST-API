using Item_Trading_App_REST_API.Models.Base;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Models.Item
{
    public class ItemsResult : BaseResult
    {
        public IEnumerable<string> ItemsId { get; set; }
    }
}
