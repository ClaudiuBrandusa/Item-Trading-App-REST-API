using Application.Models;

namespace Application.Results.Items;

public record ItemsResult : Result
{
    public IEnumerable<string> ItemsId { get; set; } = Array.Empty<string>();
}
