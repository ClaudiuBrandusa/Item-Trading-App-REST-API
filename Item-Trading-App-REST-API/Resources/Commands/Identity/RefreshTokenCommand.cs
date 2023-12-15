using Item_Trading_App_REST_API.Models.Identity;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Commands.Identity;

public record RefreshTokenCommand : IRequest<AuthenticationResult>
{
    public string Token { get; set; }

    public string RefreshToken { get; set; }
}
