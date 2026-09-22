using HW3.Models;

namespace HW3.Services;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Product> AddAsync(Product product, CancellationToken ct = default);
}
