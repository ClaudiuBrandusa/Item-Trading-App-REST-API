using MediatR;

namespace Item_Trading_App_REST_API.Resources.Commands.Wallet;

public record TakeCashCommand : IRequest<bool>
{
    public string UserId { get; set; }

    public int Amount { get; set; }
}
