using Item_Trading_App_REST_API.Models.Identity;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Commands.Identity;

public record LoginCommand : IRequest<AuthenticationResult>
{
    public string Username { get; set; }

    public string Password { get; set; }
}
