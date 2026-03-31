namespace Application.Results.Identity;

public record UsersResult
{
    public IEnumerable<string> UsersId { get; set; } = Array.Empty<string>();
}
