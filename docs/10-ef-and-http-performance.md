# Производительность EF Core и устойчивость HTTP

Материал из HW8.

## Проблема N+1

Плохой вариант:

```csharp
var customers = await db.Customers.ToListAsync();
foreach (var customer in customers)
{
    var orders = await db.Orders
        .Where(x => x.CustomerId == customer.Id)
        .ToListAsync();
}
```

Для 100 клиентов получается 101 запрос. Решение — проекция, подходящий `Include` или заранее сформированный запрос.

## Проекция

```csharp
var result = await db.Customers
    .AsNoTracking()
    .Select(customer => new CustomerOrdersDto
    {
        Id = customer.Id,
        Name = customer.Name,
        OrdersCount = customer.Orders.Count(),
        TotalSum = customer.Orders
            .Sum(order => (decimal?)order.Total) ?? 0m
    })
    .ToListAsync();
```

EF преобразует `Count`/`Sum` в SQL и загружает только нужные поля. Nullable cast позволяет получить 0 для клиента без заказов.

## `AsNoTracking`

Для GET/отчётов tracking не нужен:

```csharp
var products = await db.Products
    .AsNoTracking()
    .ToListAsync();
```

Это уменьшает память и работу change tracker. Для последующего изменения сущность нужно загрузить с tracking или явно attach.

## `Include`, cartesian explosion и split query

```csharp
var customers = await db.Customers
    .Include(x => x.Orders)
        .ThenInclude(x => x.Items)
    .AsSplitQuery()
    .AsNoTracking()
    .ToListAsync();
```

Большой JOIN нескольких коллекций повторяет родительские данные. `AsSplitQuery` делает несколько запросов, уменьшая дублирование, но увеличивая round trips. Выбор подтверждают измерениями.

## Сырой SQL и отчёты

Оконные функции, сложная агрегация и отчёты иногда проще на SQL/Dapper. Всегда использовать параметры, а не конкатенацию, чтобы избежать SQL injection и помочь reuse query plan.

## Измерения

```csharp
var sw = Stopwatch.StartNew();
var result = await service.GetOrdersAsync(ct);
sw.Stop();
logger.LogInformation("Запрос выполнен за {ElapsedMs} мс", sw.ElapsedMilliseconds);
```

Измерять следует не только время, но и число SQL-запросов, объём данных, allocations, память и поведение под нагрузкой.

## `IHttpClientFactory`

Она управляет lifetime handlers/соединений и централизует конфигурацию:

```csharp
services.AddHttpClient<IProductApi, ProductApi>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
```

Новый `HttpClient` на каждый запрос может исчерпать сокеты; один вечный самодельный client может долго хранить старые DNS-данные. Factory управляет handler pool.

## `DelegatingHandler`

Это middleware для исходящих HTTP-запросов. Применения: correlation ID, метрики, авторизация, логирование.

```csharp
services.AddTransient<CorrelationHandler>();
services.AddHttpClient<IProductApi, ProductApi>()
    .AddHttpMessageHandler<CorrelationHandler>();
```

Обработчик обязан вызвать `base.SendAsync`, иначе запрос не уйдёт дальше.

## Timeout

```csharp
using var timeoutCts = CancellationTokenSource
    .CreateLinkedTokenSource(cancellationToken);
timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

try
{
    return await api.GetProductAsync(id, timeoutCts.Token);
}
catch (OperationCanceledException)
    when (!cancellationToken.IsCancellationRequested)
{
    throw new TimeoutException();
}
```

Фильтр отличает внутренний timeout от отмены клиента.

## Retry

Повторяют временные ошибки: network failure, 408, 429, часть 5xx. Обычно применяют exponential backoff + jitter. Не следует повторять 400/401/403/404 и изменяющие запросы без идемпотентности.

## Circuit breaker

Состояния:

1. **Closed** — запросы проходят.
2. **Open** — запросы сразу отклоняются, зависимый сервис отдыхает.
3. **Half-open** — пробный запрос проверяет восстановление.

Circuit breaker уменьшает каскадные сбои и не тратит ресурсы на заведомо неисправимую зависимость.

## Идемпотентность и retry

GET обычно безопасен для повтора. POST заказа/платежа — нет. Один `Idempotency-Key` должен жить на протяжении всех retry одной логической операции. Надёжный сервер атомарно резервирует ключ, связывает с пользователем и hash тела, сохраняет статус/body/headers и использует общее durable storage.

## Общая стратегия

```text
Caller cancellation
  -> общий timeout
  -> retry временных ошибок
  -> timeout отдельной попытки
  -> circuit breaker
  -> HTTP transport
```

Точный порядок resilience handlers влияет на поведение, поэтому его надо выбирать сознательно и проверять тестами.
