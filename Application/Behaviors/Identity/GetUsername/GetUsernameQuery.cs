using MediatR;

namespace Application.Behaviors.Identity.GetUsername;

public record GetUsernameQuery : IRequest<Result<string>>
{
    public string UserId { get; set; } = string.Empty;
}
