using Application.Models.Identity;
using MediatR;

namespace Application.Behaviors.Identity.LoginUser;

public record LoginCommand : IRequest<AuthenticationResult>
{
    public string Username { get; set; }

    public string Password { get; set; }
}
