using MediatR;

namespace Application.Behaviors.Wallet.TakeCash;

public record TakeCashCommand : IRequest<bool>
{
    public string UserId { get; set; }

    public int Amount { get; set; }
}
