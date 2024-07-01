using Application.Behaviors.Wallet.UpdateWallet;
using Item_Trading_App_Contracts.Requests.Wallet;
using Mapster;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class WalletMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<UpdateWalletRequest, UpdateWalletCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(UpdateWalletCommand.UserId)].ToString());
    }
}
