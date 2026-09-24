using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Abstractions;
using Shop.Domain;

namespace Shop.Infrastructure.Repositories;

public sealed class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct) =>
        db.Customers.SingleOrDefaultAsync(customer => customer.Email == email, ct);

    public void Add(Customer customer) => db.Customers.Add(customer);
}

public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<Product?> GetAsync(Guid id, CancellationToken ct) =>
        db.Products.SingleOrDefaultAsync(product => product.Id == id, ct);

    public void Add(Product product) => db.Products.Add(product);
}

public sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetAsync(Guid id, CancellationToken ct) =>
        db.Orders.Include(order => order.Items).SingleOrDefaultAsync(order => order.Id == id, ct);

    public void Add(Order order) => db.Orders.Add(order);
}
