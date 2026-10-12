# Minimal API и OpenAPI/Swagger

Материал из HW6. Это отдельное in-memory приложение каталога, а не прямое продолжение полной архитектуры HW5.

## Minimal API

Вместо классов-контроллеров endpoint'ы описаны в `Program.cs`:

```csharp
app.MapGet("/api/products", () => Results.Ok(products));

app.MapGet("/api/products/{id:guid}", (Guid id) =>
    products.FirstOrDefault(x => x.Id == id) is { } product
        ? Results.Ok(product)
        : Results.NotFound());
```

Плюсы: мало шаблонного кода, удобно для небольших API и микросервисов. При росте приложения обработчики лучше выносить в отдельные классы/методы, чтобы `Program.cs` не превращался в монолит.

## Регистрация Swagger

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Shop Management API",
        Version = "1.0.0",
        Description = "API для управления каталогом товаров",
        Contact = new OpenApiContact
        {
            Name = "Shop API Support",
            Email = "support@shop-api.example"
        },
        License = new OpenApiLicense { Name = "MIT" }
    });
});
```

Middleware публикации документа и UI:

```csharp
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Shop Management API v1");
    options.RoutePrefix = "swagger";
});
```

- `/swagger/v1/swagger.json` — машиночитаемый OpenAPI JSON;
- `/swagger` — интерактивный интерфейс.

## Описание endpoint'ов

```csharp
app.MapPost("/api/products", CreateProduct)
    .WithName("CreateProduct")
    .WithTags("Products")
    .WithSummary("Добавить товар в каталог")
    .Produces<CatalogProductResponse>(StatusCodes.Status201Created)
    .ProducesValidationProblem();
```

- `WithName` даёт уникальный operation name;
- `WithTags` группирует операции;
- `WithSummary` добавляет краткое описание;
- `Produces<T>` документирует статус и схему ответа;
- `ProducesValidationProblem` описывает ошибку валидации.

Документация полезна только тогда, когда совпадает с реальным поведением.

## Ручная валидация Minimal API

```csharp
var errors = new Dictionary<string, string[]>();

if (string.IsNullOrWhiteSpace(request.Name))
    errors["name"] = ["Название обязательно."];
if (request.Price <= 0)
    errors["price"] = ["Цена должна быть больше нуля."];

if (errors.Count > 0)
    return Results.ValidationProblem(errors);
```

При успехе:

```csharp
return Results.Created($"/api/products/{product.Id}", product);
```

Для большого проекта проверку лучше вынести в endpoint filter или валидатор.

## Проверка

```bash
dotnet run --project Homework/HW6/HW6.csproj --urls http://localhost:5086
curl -i http://localhost:5086/swagger/v1/swagger.json
curl -i http://localhost:5086/api/products
curl -i -X POST http://localhost:5086/api/products \
  -H 'Content-Type: application/json' \
  -d '{"name":"Веб-камера","price":6500,"stock":8}'
```

Данные хранятся в обычном `List<T>`, поэтому исчезают при перезапуске. Кроме того, список не защищён от конкурентной записи; это демонстрация API-контракта, а не production storage.

## Что OpenAPI даёт проекту

- интерактивную ручную проверку;
- генерацию типизированных клиентов;
- согласование контракта backend/frontend;
- основу для contract tests;
- описание схем DTO, параметров и статусов.

Нельзя публиковать Swagger UI наружу без решения по безопасности: документ может раскрывать внутренние endpoint'ы. Сам OpenAPI следует версионировать вместе с API.
