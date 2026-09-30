namespace BookStore.Api.Models;

public class CreateBookV2Request
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public Money Price { get; set; } = new();
    public string? Isbn { get; set; }
}
