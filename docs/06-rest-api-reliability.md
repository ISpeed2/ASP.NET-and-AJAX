# REST API: контракты, ошибки, идемпотентность и ETag

## DTO и HTTP-контракт

API использует отдельные записи:

```csharp
public sealed record CreateProductRequest(string Name, decimal Price, int Stock);
public sealed record UpdateProductRequest(string Name, decimal Price, int Stock);
public sealed record PatchProductRequest(string? Name, decimal? Price, int? Stock);
public sealed record ProductResponse(Guid Id, string Name, decimal Price, int Stock);
```

Разделение защищает сущность от over-posting и явно описывает, какие поля допустимы в POST, PUT, PATCH и ответе.

## CRUD и статусы

- `GET /api/products` — `200 OK`, пагинация через `page`/`pageSize`;
- `GET /api/products/{id}` — `200` или `404`;
- `POST /api/products` — `201 Created` и `Location`;
- `PUT /api/products/{id}` — полная замена изменяемых полей;
- `PATCH /api/products/{id}` — частичное изменение;
- `DELETE /api/products/{id}` — `204 No Content`.

Пагинация ограничивает размер страницы:

```csharp
page = Math.Max(page, 1);
pageSize = Math.Clamp(pageSize, 1, 100);

var result = await db.Products
    .AsNoTracking()
    .OrderBy(x => x.Name)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(x => new ProductResponse(x.Id, x.Name, x.Price, x.Stock))
    .ToListAsync(ct);
```

Стабильная сортировка обязательна: без `OrderBy` страницы могут меняться между запросами.

## Custom Model Binder

`CsvGuidArrayBinder` превращает query string `?ids=id1,id2` в `Guid[]`. При неверном GUID добавляет ошибку в `ModelState`. `[ApiController]` автоматически формирует 400 для ошибок binding/validation.

## FluentValidation

```csharp
RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(100);
RuleFor(x => x.Price).GreaterThan(0).LessThan(1_000_000);
RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);

When(x => x.Price > 10_000, () =>
    RuleFor(x => x.Name).MinimumLength(10));
```

FluentValidation удобен для условных и составных правил. Техническая валидация DTO дополняет доменные инварианты, но не заменяет их.

## Problem Details

Ошибки приводятся к RFC-совместимому объекту:

```json
{
  "title": "product.out_of_stock",
  "status": 409,
  "detail": "Недостаточно товара",
  "instance": "/api/orders",
  "errorType": "Conflict"
}
```

`GlobalExceptionHandler` перехватывает неожиданные исключения, логирует их и добавляет `traceId`. В production нельзя бездумно отдавать `exception.Message`: оно может раскрыть внутренние детали.

## Идемпотентность POST

Клиент передаёт один ключ на одну логическую операцию:

```http
Idempotency-Key: 3c7869a0-5c37-4a88-9fad-8266bd24cc77
```

Сервер HW4/HW5 хранит первый ответ 24 часа и возвращает его повторно. Идея предотвращает дубликаты при retry.

Учебный `ConcurrentDictionary` потокобезопасен, но реализация неполная:

- проверка и выполнение не атомарны — два одновременных запроса могут оба создать товар;
- не проверяется совпадение тела запроса;
- ключ не привязан к пользователю;
- данные исчезают после рестарта и не разделяются между серверами;
- просроченные элементы явно не очищаются;
- сохранённый `Location` не восстанавливается при повторном ответе.

В production обычно используют транзакционную таблицу или Redis с атомарной резервацией ключа.

## Conditional GET через ETag

HW5 формирует версию:

```csharp
public static string For(Product product) =>
    $"\"product-{product.Id:N}-v{product.Version}\"";
```

Сервер возвращает:

```http
ETag: "product-<id>-v1"
```

Клиент повторяет GET с `If-None-Match`. Если версия не изменилась, сервер отвечает `304 Not Modified` без тела.

## Optimistic concurrency через `If-Match`

Для PUT/PATCH/DELETE HW5 требует актуальный ETag:

- заголовка нет — `428 Precondition Required`;
- ETag устарел — `412 Precondition Failed`;
- ETag актуален — изменение выполняется и возвращается новая версия.

```bash
curl -X PUT http://localhost:5085/api/products/ID \
  -H 'Content-Type: application/json' \
  -H 'If-Match: "product-ID-v1"' \
  -d '{"name":"Новая клавиатура","price":5000,"stock":9}'
```

Поле `Version` настроено как EF concurrency token:

```csharp
modelBuilder.Entity<Product>()
    .Property(product => product.Version)
    .IsConcurrencyToken();
```

При конфликте БД `DbUpdateConcurrencyException` перехватывается, tracker очищается, актуальная версия читается заново и возвращается клиенту.

## Разница HW4, HW5 и HW6

- HW4 содержит основные REST-механики, но разрешает изменение без `If-Match` и сравнивает ETag упрощённо.
- HW5 делает условие обязательным, поддерживает список тегов/`*`, ловит EF concurrency conflict и возвращает подробные 412/428.
- HW6 не продолжает HW5: это отдельный небольшой in-memory Minimal API ради OpenAPI. В нём ETag-реализация снова упрощённая в оставшихся старых файлах, но фактически запускается новый `Program.cs` Minimal API.

## Памятка

- Использовать DTO, а не выдавать EF entity напрямую.
- Возвращать точные HTTP-статусы и единый Problem Details.
- POST с retry защищать серверной идемпотентностью.
- Конкурентное редактирование защищать ETag + concurrency token.
- `AsNoTracking` применять для read-only запросов.
- Не путать `PUT` (полная замена) и `PATCH` (частичное изменение).
