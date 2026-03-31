namespace Application.Results.Items;

public record ItemsResult
{
    public IEnumerable<string> ItemsId { get; set; } = Array.Empty<string>();
}
