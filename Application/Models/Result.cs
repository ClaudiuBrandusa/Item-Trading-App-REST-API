namespace Application.Models;

public record Result
{
    public bool Success { get; set; }

    public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();
}
