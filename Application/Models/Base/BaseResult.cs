namespace Application.Models.Base;

public record BaseResult
{
    public bool Success { get; set; }

    public IEnumerable<string> Errors { get; set; }
}
