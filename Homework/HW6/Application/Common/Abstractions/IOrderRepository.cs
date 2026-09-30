using Shop.Domain;

namespace Shop.Application.Common.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetAsync(Guid id, CancellationToken ct);
    void Add(Order order);
}
