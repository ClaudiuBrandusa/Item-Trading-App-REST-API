using Application.Results.Identity;
using MediatR;

namespace Application.Behaviors.Identity.RefreshToken;

public record RefreshTokenCommand : IRequest<Result<AuthenticationResult>>
{
    public string Token { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;
}
