# Многослойная архитектура, EF Core и заказы

Материал основан на почти одинаковой базе HW4/HW5. HW5 развивает concurrency, а HW6 уже является отдельным упрощённым Minimal API.

## Слои проекта

```text
Web / API
   |
Application (use cases, Result, interfaces)
   |
Domain (Customer, Product, Order, OrderItem)
   ^
Infrastructure (EF Core, repositories, SQLite)
```

- **Domain** содержит бизнес-состояние и инварианты.
- **Application** координирует сценарии, не привязываясь к HTTP.
- **Infrastructure** реализует хранение.
- **Web/API** переводит HTTP-ввод в команды и результат — в HTML/HTTP.

## Богатые доменные модели

`Product` защищает свои свойства private setter'ами и меняется методами:

```csharp
public void DecreaseStock(int quantity)
{
    if (quantity <= 0)
        throw new ArgumentException("Quantity must be positive");
    if (Stock < quantity)
        throw new InvalidOperationException("Not enough stock");

    Stock -= quantity;
}
```

`Order` сам вычисляет `Total`, создаёт ID и управляет переходами статуса:

```csharp
public void Cancel()
{
    if (Status == OrderStatus.Shipped)
        throw new InvalidOperationException("Cannot cancel shipped order");
    if (Status == OrderStatus.Cancelled)
        return;
    Status = OrderStatus.Cancelled;
}
```

Так правила нельзя случайно обойти прямым присваиванием из контроллера.

## Use case создания заказа

Последовательность `CreateOrderUseCase`:

1. проверить email и наличие позиций;
2. найти клиента;
3. запретить заказ заблокированному клиенту;
4. найти каждый товар;
5. проверить остаток и уменьшить его;
6. создать снимок `OrderItem` с названием и текущей ценой;
7. добавить заказ;
8. один раз вызвать `SaveChangesAsync`.

Упрощённый фрагмент:

```csharp
foreach (var item in command.Items)
{
    var product = await products.GetAsync(item.ProductId, ct);
    if (product is null)
        return Error.NotFound("product.not_found", "Товар не найден");

    if (product.Stock < item.Quantity)
        return Error.Conflict("product.out_of_stock", "Недостаточно товара");

    product.DecreaseStock(item.Quantity);
    orderItems.Add(new OrderItem(
        product.Id, product.Name, product.Price, item.Quantity));
}
```

Цена копируется в заказ, чтобы исторический заказ не менялся после смены цены товара.

## `Result<T>` вместо ожидаемых исключений

```csharp
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
}
```

Ожидаемые бизнес-исходы возвращаются как `Validation`, `NotFound`, `Conflict`, `Forbidden`. API преобразует их в 400/404/409/403, а MVC — в сообщение формы или соответствующий ActionResult.

Исключения остаются для неожиданных ошибок или нарушений инвариантов внутри доменной модели.

## EF Core и SQLite

`AppDbContext` содержит:

```csharp
public DbSet<Customer> Customers => Set<Customer>();
public DbSet<Product> Products => Set<Product>();
public DbSet<Order> Orders => Set<Order>();
public DbSet<OrderItem> OrderItems => Set<OrderItem>();
```

`DbContext` одновременно реализует `IUnitOfWork`: все изменения запроса сохраняются одним `SaveChangesAsync`, обычно в одной транзакции.

Репозитории скрывают детали запроса:

```csharp
public Task<Order?> GetAsync(Guid id, CancellationToken ct) =>
    db.Orders
      .Include(order => order.Items)
      .SingleOrDefaultAsync(order => order.Id == id, ct);
```

`Include` нужен, потому что детали заказа используют его позиции.

## Конфигурация модели

Часть настроек читается из `entity-config.json`: таблицы, ключи, required, длины, индексы и типы колонок. `Order` настроен Fluent API вручную, включая backing field `_items` и owned collection:

```csharp
builder.Metadata.FindNavigation(nameof(Order.Items))!
    .SetPropertyAccessMode(PropertyAccessMode.Field);

builder.OwnsMany<OrderItem>("_items", owned =>
{
    owned.ToTable("OrderItems");
    owned.WithOwner().HasForeignKey("OrderId");
});
```

JSON-конфиг демонстрирует data-driven настройку, но в большом проекте обычные `IEntityTypeConfiguration<T>` типобезопаснее и проще рефакторятся.

## Инициализация данных

`DbInitializer.SeedAsync` вызывает `EnsureCreatedAsync`, затем добавляет демонстрационного клиента и товары, только если таблицы пусты.

Для production лучше миграции (`Database.MigrateAsync`) и отдельный управляемый процесс seed: `EnsureCreated` не предназначен для развития схемы через миграции.

## MVC и API поверх одних use cases

MVC `OrdersController` создаёт `CreateOrderCommand`, показывает ошибки через `ModelState`, а при успехе делает redirect на details.

API `OrdersApiController` создаёт ту же команду, но возвращает:

```csharp
CreatedAtAction(nameof(GetById), new { id }, new { id })
```

Это `201 Created` с `Location` на созданный ресурс. Оба интерфейса используют один application layer, поэтому бизнес-правила не дублируются.

## Замечания по текущему коду

- В `CancelOrderUseCase` внедрён конкретный `AppDbContext`, хотя уже существует `IUnitOfWork`; это связывает Application с Infrastructure.
- Папка названа `CancleOrder`, а namespace — `CancelOrder`: опечатку лучше исправить.
- Создание заказа делает N отдельных запросов товаров; список можно загрузить одним запросом и проверить в памяти.
- При параллельных заказах остаток требует concurrency-защиты/транзакционной стратегии.
- При отмене заказа текущий код не возвращает остаток товара — это должно быть осознанным бизнес-решением.
