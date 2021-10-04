﻿using Item_Trading_App_REST_API.Models.Base;

namespace Item_Trading_App_REST_API.Models.Item
{
    public class DeleteItemResult : BaseResult
    {
        public string ItemId { get; set; }

        public string ItemName { get; set; }
    }
}
