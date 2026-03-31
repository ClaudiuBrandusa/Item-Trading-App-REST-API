namespace Application.Results.Items;

public record ShortItemResult
{
    public string ItemId { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;
}
