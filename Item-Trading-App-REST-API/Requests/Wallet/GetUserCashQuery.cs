using MediatR;

namespace Item_Trading_App_REST_API.Requests.Wallet;

public record GetUserCashQuery : IRequest<int>
{
    public string UserId { get; set; }
}
