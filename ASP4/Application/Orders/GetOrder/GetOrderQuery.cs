using Shop.Domain;

namespace Shop.Application.Orders.GetOrder;

public sealed record GetOrderQuery(Guid OrderId);

public sealed record OrderDto(
    Guid Id,
    string CustomerEmail,
    OrderStatus Status,
    DateTime CreatedAtUtc,
    decimal Total,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderItemDto(Guid ProductId, string Name, decimal Price, int Quantity);