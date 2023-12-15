using Item_Trading_App_REST_API.Models.Wallet;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Commands.Wallet;

public record UpdateWalletCommand : IRequest<WalletResult>
{
    public string UserId { get; set; }

    public int Quantity { get; set; }
}
