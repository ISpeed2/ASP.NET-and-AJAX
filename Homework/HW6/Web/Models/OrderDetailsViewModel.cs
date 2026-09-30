namespace ASP4.Web.Models;

public sealed class OrderDetailsViewModel
{
    public Guid Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool CanBeCancelled { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = new();
}

public sealed class OrderItemViewModel
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
