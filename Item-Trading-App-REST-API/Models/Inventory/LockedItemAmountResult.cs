using Item_Trading_App_REST_API.Models.Base;

namespace Item_Trading_App_REST_API.Models.Inventory
{
    public class LockedItemAmountResult : BaseResult
    {
        public string ItemId { get; set; }

        public string ItemName { get; set; }

        public int Amount { get; set; }
    }
}
