using Item_Trading_App_REST_API.Models.Base;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Models.Item
{
    public class InventoryItemsResult : BaseResult
    {
        public IEnumerable<QuantifiedItemResult> Items { get; set; }
    }
}
