namespace Shop.Application.Orders.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId);