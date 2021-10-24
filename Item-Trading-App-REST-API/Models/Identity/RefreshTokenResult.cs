using Item_Trading_App_REST_API.Models.Base;
using System;

namespace Item_Trading_App_REST_API.Models.Identity
{
    public class RefreshTokenResult : BaseResult
    {
        public string Token { get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool Used { get; set; }

        public bool Invalidated { get; set; }

        public string UserId { get; set; }
    }
}
