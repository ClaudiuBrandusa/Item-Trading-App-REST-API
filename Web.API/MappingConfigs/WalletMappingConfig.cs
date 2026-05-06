using Application.Behaviors.Wallet.GetWallet;
using Application.Behaviors.Wallet.UpdateWallet;
using Application.Results.Wallet;
using Item_Trading_App_Contracts.Requests.Wallet;
using Item_Trading_App_Contracts.Responses.Wallet;
using Mapster;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class WalletMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<UpdateWalletRequest, UpdateWalletCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(UpdateWalletCommand.UserId)].ToString());

        config.ForType<string, GetUserWalletQuery>()
            .MapWith(str =>
                new GetUserWalletQuery { UserId = str });

        config.ForType<WalletResult, UpdateWalletSuccessResponse>()
            .Map(dest => dest.Amount, src => src.Cash);
    }
}
