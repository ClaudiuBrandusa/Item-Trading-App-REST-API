using Item_Trading_App_REST_API.Models.Wallet;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.Wallet;

public record GetUserWalletQuery : IRequest<WalletResult>
{
    public string UserId { get; set; }
}
