# Middleware и жизненный цикл HTTP-запроса

Материал реализован в `Homework/HW2`.

## Полный путь запроса

```text
Клиент
  -> RequestLifecycleMiddleware (до next)
  -> Routing
  -> HomeController
  -> Model Binding + Validation
  -> Razor View
  -> RequestLifecycleMiddleware (после next)
  -> Клиент
```

Middleware работает как вложенные слои. Код до `_next` выполняется по пути к endpoint, а код после — в обратном направлении.

## Реализация

```csharp
public sealed class RequestLifecycleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLifecycleMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;

        logger.LogInformation(
            "Request received: {Method} {Path}, TraceId={TraceId}",
            context.Request.Method,
            context.Request.Path,
            requestId);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Response sent: {ElapsedMilliseconds} ms, TraceId={TraceId}",
                stopwatch.ElapsedMilliseconds,
                requestId);
        }
    }
}
```

`finally` гарантирует измерение времени даже при исключении ниже по конвейеру.

## Регистрация

```csharp
app.UseHttpsRedirection();
app.UseMiddleware<RequestLifecycleMiddleware>();
app.UseRouting();
app.UseAuthorization();
```

В таком положении middleware видит почти все запросы до маршрутизации. Если компоненту нужны route values или endpoint metadata, его нужно поставить после `UseRouting()`.

## `HttpContext`

Через него доступны:

- `Request.Method`, `Request.Path`, headers и body;
- `Response.StatusCode`, headers и body;
- `User` — текущий principal;
- `TraceIdentifier` — ID запроса;
- `RequestServices` — scoped DI-контейнер;
- `RequestAborted` — токен отмены клиента.

## Заголовки ответа и `OnStarting`

В HW2 middleware добавляет:

```csharp
context.Response.Headers["X-Lifecycle-TraceId"] = requestId;
context.Response.OnStarting(() =>
{
    context.Response.Headers["X-Lifecycle-Stage"] =
        "Response returned from controller";
    return Task.CompletedTask;
});
```

`OnStarting` вызывается непосредственно перед отправкой заголовков. После начала ответа менять headers уже нельзя.

## Структурированное логирование

Шаблон с именованными параметрами лучше интерполяции:

```csharp
logger.LogInformation(
    "Completed {StatusCode}, TraceId={TraceId}",
    context.Response.StatusCode,
    context.TraceIdentifier);
```

Провайдер логирования получает отдельные поля `StatusCode` и `TraceId`, по которым можно фильтровать записи.

## Short-circuit

Если middleware не вызовет `_next(context)`, дальнейшая цепочка не выполнится. Это применяется для:

- аутентификации/отказа в доступе;
- rate limiting;
- кешированного ответа;
- проверки обязательного заголовка;
- обслуживания статического файла.

## Типичные ошибки

- добавлять заголовок после того, как response уже начался;
- читать request body без buffering, лишая контроллер возможности прочитать его;
- логировать пароли, Cookie, токены или полный конфиденциальный body;
- проглатывать исключение и оставлять непонятный HTTP 200;
- ставить middleware в неверном порядке.

## Практический итог HW2

Форма `RegistrationRequest` проходит binding и Data Annotations, контроллер возвращает `Index` при ошибках или `Success` при успехе, а middleware независимо от MVC записывает метод, путь, статус, trace ID и длительность. Это пример cross-cutting concern: наблюдаемость реализована один раз для всех endpoint'ов.
