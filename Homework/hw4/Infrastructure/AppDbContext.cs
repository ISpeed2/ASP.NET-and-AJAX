using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Abstractions;
using Shop.Domain;
using Shop.Infrastructure.Persistence.Configurations.Json;

namespace Shop.Infrastructure;

public sealed class AppDbContext : DbContext, IUnitOfWork
{
    private static readonly string JsonPath = Path.Combine(
        AppContext.BaseDirectory,
        "Infrastructure",
        "Persistence",
        "Configurations",
        "entity-config.json");

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        JsonSimpleConfigurator.Apply(modelBuilder, JsonPath, typeof(Order).Assembly);
        ConfigureOrder(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("Orders");
            builder.HasKey(order => order.Id);
            builder.Property(order => order.CustomerEmail).IsRequired().HasMaxLength(256);
            builder.Property(order => order.Status).HasConversion<int>().IsRequired();
            builder.Property(order => order.Total).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(order => order.CreatedAtUtc).IsRequired();
            builder.HasIndex(order => order.CustomerEmail);

            builder.Metadata.FindNavigation(nameof(Order.Items))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.OwnsMany<OrderItem>("_items", owned =>
            {
                owned.ToTable("OrderItems");
                owned.WithOwner().HasForeignKey("OrderId");
                owned.HasKey(item => item.Id);
                owned.Property(item => item.ProductId).IsRequired();
                owned.Property(item => item.Name).IsRequired().HasMaxLength(200);
                owned.Property(item => item.Price).HasColumnType("decimal(18,2)").IsRequired();
                owned.Property(item => item.Quantity).IsRequired();
            });
        });
        modelBuilder.Entity<Product>().Property(product => product.Version).IsConcurrencyToken();
    }
}
