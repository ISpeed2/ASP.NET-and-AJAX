using Shop.Application.Common;
using Shop.Application.Common.Abstractions;
using Shop.Domain;
using Shop.Infrastructure;

namespace Shop.Application.Orders.CancelOrder;

public sealed class CancelOrderUseCase
{
    private readonly IOrderRepository _orders;
    private readonly AppDbContext _db;

    public CancelOrderUseCase(IOrderRepository orders, AppDbContext db)
    {
        _orders = orders; _db = db;
    }

    public async Task<Result<bool>> ExecuteAsync(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await _orders.GetAsync(cmd.OrderId, ct);
        if (order is null) return Error.NotFound("order.not_found", "Заказ не найден");
        if (order.Status == OrderStatus.Shipped)
            return Error.Conflict("order.shipped", "Отгруженный заказ нельзя отменить");

        order.Cancel();
        await _db.SaveChangesAsync(ct);

        return true;
    }
}