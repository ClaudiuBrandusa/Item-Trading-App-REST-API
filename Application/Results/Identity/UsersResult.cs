using Application.Models;

namespace Application.Results.Identity;

public record UsersResult : Result
{
    public IEnumerable<string> UsersId { get; set; } = Array.Empty<string>();
}
