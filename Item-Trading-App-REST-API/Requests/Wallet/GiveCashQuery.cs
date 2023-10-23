using MediatR;

namespace Item_Trading_App_REST_API.Requests.Wallet;

public class GiveCashQuery : IRequest<bool>
{
    public string UserId { get; set; }

    public int Amount { get; set; }
}
