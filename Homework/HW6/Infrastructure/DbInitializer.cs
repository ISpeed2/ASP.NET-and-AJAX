using Microsoft.EntityFrameworkCore;
using Shop.Domain;

namespace Shop.Infrastructure;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        if (!await db.Customers.AnyAsync(ct))
            db.Customers.Add(new Customer("student@example.com", "Учебный клиент"));

        if (!await db.Products.AnyAsync(ct))
        {
            db.Products.AddRange(
                new Product("Клавиатура", 4500m, 10),
                new Product("Мышь", 2500m, 20),
                new Product("Монитор", 25000m, 5));
        }

        await db.SaveChangesAsync(ct);
    }
}
