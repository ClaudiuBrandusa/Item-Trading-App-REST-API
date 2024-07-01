using MediatR;

namespace Application.Behaviors.Identity.GetUsername;

public record GetUsernameQuery : IRequest<string>
{
    public string UserId { get; set; }
}
