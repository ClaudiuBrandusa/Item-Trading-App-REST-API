using Application.Behaviors.Identity.LoginUser;

namespace Application.Behaviors.Identity.RegisterUser;

public record RegisterCommand : LoginCommand
{
    public string Email { get; set; } = string.Empty;
}
