namespace Application.Results.Items;

public record DeleteItemResult
{
    public string ItemId { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;
}
