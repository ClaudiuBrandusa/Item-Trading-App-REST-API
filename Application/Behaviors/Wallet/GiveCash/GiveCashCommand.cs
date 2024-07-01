using MediatR;

namespace Application.Behaviors.Wallet.GiveCash;

public record GiveCashCommand : IRequest<bool>
{
    public string UserId { get; set; }

    public int Amount { get; set; }
}
