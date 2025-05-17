using MediatR;

namespace Application.Behaviors.Wallet.GetCash;

public record GetUserCashQuery : IRequest<int>
{
    public string UserId { get; set; } = string.Empty;
}
