using Item_Trading_App_Contracts.Requests.Wallet;
using Item_Trading_App_REST_API.Resources.Commands.Wallet;
using Item_Trading_App_REST_API.Resources.Queries.Wallet;
using Mapster;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class WalletMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<UpdateWalletRequest, UpdateWalletCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(UpdateWalletCommand.UserId)].ToString());

        config.ForType<GiveCashCommand, UpdateWalletCommand>()
            .Map(dest => dest.Quantity, src => src.Amount);

        config.ForType<string, GetUserWalletQuery>()
            .MapWith(str =>
                new GetUserWalletQuery { UserId = str });
    }
}
