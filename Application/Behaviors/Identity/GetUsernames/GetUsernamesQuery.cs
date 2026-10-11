using MediatR;

namespace Application.Behaviors.Identity.GetUsername;

public record GetUsernamesQuery : IRequest<Result<Dictionary<string, string>>>
{
    public string[] UserIds { get; set; } = [];
}
