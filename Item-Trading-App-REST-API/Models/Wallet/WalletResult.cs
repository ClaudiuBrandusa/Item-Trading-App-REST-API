using Item_Trading_App_REST_API.Models.Base;

namespace Item_Trading_App_REST_API.Models.Wallet
{
    public class WalletResult : BaseResult
    {
        public string UserId { get; set; }

        public int Cash { get; set; }
    }
}
