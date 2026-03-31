using Application.Results.Identity;
using MediatR;

namespace Application.Behaviors.Identity.LoginUser;

public record LoginCommand : IRequest<Result<AuthenticationResult>>
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
