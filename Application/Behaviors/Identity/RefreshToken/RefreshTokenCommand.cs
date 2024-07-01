using Application.Models.Identity;
using MediatR;

namespace Application.Behaviors.Identity.RefreshToken;

public record RefreshTokenCommand : IRequest<AuthenticationResult>
{
    public string Token { get; set; }

    public string RefreshToken { get; set; }
}
