using Item_Trading_App_Contracts.Requests.Wallet;
using Item_Trading_App_REST_API.Models.Wallet;
using Item_Trading_App_REST_API.Requests.Wallet;
using Mapster;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class WalletMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<UpdateWalletRequest, UpdateWallet>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters["userId"].ToString());

        config.ForType<GiveCashQuery, UpdateWallet>()
            .Map(dest => dest.Quantity, src => src.Amount);
    }
}
