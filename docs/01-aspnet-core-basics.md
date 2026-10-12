# Основы ASP.NET Core в проекте

## Что использовалось

- ASP.NET Core MVC и Minimal API;
- встроенный Dependency Injection;
- middleware pipeline;
- attribute routing и conventional routing;
- Razor Views и статические файлы;
- конфигурация через `appsettings.json`;
- логирование через `ILogger<T>`;
- асинхронные операции и `CancellationToken`.

## Точка входа `Program.cs`

Современное приложение ASP.NET Core собирается в два этапа:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IProductRepository, JsonProductRepository>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

`builder.Services` — коллекция регистраций DI. После `Build()` создаётся приложение и настраивается обработка HTTP-запросов.

## Время жизни зависимостей

В исходниках встречаются три основных lifetime:

- `AddSingleton` — один объект на всё время жизни процесса. Так зарегистрированы JSON-репозиторий, in-memory store идемпотентности, хранилища сессий и refresh token. Singleton должен быть потокобезопасным.
- `AddScoped` — один объект на HTTP-запрос. Так зарегистрированы `DbContext`, репозитории и use case классы HW4/HW5.
- `AddTransient` — новый объект при каждом запросе зависимости. В конспектах HW8 так предлагается регистрировать `DelegatingHandler`.

Пример корректной регистрации EF Core:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<CreateOrderUseCase>();
```

## Middleware pipeline

Middleware выполняются в порядке добавления. Каждый компонент может:

1. обработать запрос до следующего компонента;
2. вызвать `await next(context)`;
3. обработать ответ после возврата управления;
4. прекратить цепочку, не вызывая `next`.

Порядок важен. Например, в HW9 сначала подключаются authentication, затем authorization:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Без первого middleware `HttpContext.User` не будет заполнен аутентифицированным пользователем, а второму нечего будет проверять.

## Маршрутизация

Conventional route из MVC-проектов:

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

URL `/Home/Details/5` сопоставляется с `HomeController.Details(5)`. Значения по умолчанию позволяют `/` открыть `HomeController.Index`.

Для API использована маршрутизация атрибутами:

```csharp
[ApiController]
[Route("api/products")]
public sealed class ProductsApiController : ControllerBase
{
    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct) { ... }
}
```

Ограничение `:guid` не пропускает строку, которая не может быть `Guid`.

Minimal API описывает маршрут напрямую:

```csharp
app.MapGet("/api/products/{id:guid}", (Guid id) => { ... });
```

## Окружения и обработка ошибок

Типичная производственная ветка:

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
```

- `UseExceptionHandler` не показывает пользователю stack trace;
- HSTS просит браузер использовать HTTPS;
- development и production могут иметь разные `appsettings.{Environment}.json`.

В API HW4/HW5 дополнительно применён `IExceptionHandler`, формирующий единый `ProblemDetails`.

## Статические файлы и UI

`UseStaticFiles()` или `MapStaticAssets()` обслуживает CSS, JavaScript и изображения из `wwwroot`. Bootstrap и jQuery в проектах — готовые клиентские зависимости. Собственные стили лежат в `wwwroot/css/site.css`, а логика — в `wwwroot/js`.

## Асинхронность и отмена

Контроллеры, репозитории и EF Core методы используют `async/await`:

```csharp
public async Task<IActionResult> Details(int id, CancellationToken ct)
{
    var product = await repository.GetByIdAsync(id, ct);
    return product is null ? NotFound() : View(product);
}
```

`CancellationToken` приходит от HTTP-запроса. Если клиент отключился, отмена может пройти до чтения файла, БД или внешнего API. Его нужно передавать по всей цепочке, иначе отменяемость теряется.

## Важные выводы

- `Program.cs` одновременно формирует DI-контейнер и HTTP-конвейер.
- Порядок middleware влияет на безопасность и поведение приложения.
- MVC-маршруты возвращают HTML, API-маршруты — обычно JSON и HTTP-статусы.
- Scoped-сервисы подходят для работы с `DbContext`; singleton обязан быть потокобезопасным.
- Секреты и ключи не должны храниться в обычном `appsettings.json` реального проекта: нужны environment variables, Secret Manager или vault.
