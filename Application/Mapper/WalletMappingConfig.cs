using Application.Behaviors.Wallet.GetWallet;
using Application.Behaviors.Wallet.GiveCash;
using Application.Behaviors.Wallet.UpdateWallet;
using Mapster;

namespace Application.Mapper;

public class WalletMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<GiveCashCommand, UpdateWalletCommand>()
            .Map(dest => dest.Quantity, src => src.Amount);

        config.ForType<string, GetUserWalletQuery>()
            .MapWith(str =>
                new GetUserWalletQuery { UserId = str });
    }
}
