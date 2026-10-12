# Channels, фоновые задачи и тонкий HTTP-клиент

## `Channel<T>`

`System.Threading.Channels` — потокобезопасная асинхронная очередь producer/consumer. Writer добавляет элементы, Reader читает, а ожидание не блокирует поток.

```csharp
var options = new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = true,
    SingleWriter = false,
    AllowSynchronousContinuations = false
};

var channel = Channel.CreateBounded<
    Func<CancellationToken, ValueTask>>(options);
```

- capacity ограничивает память;
- `Wait` создаёт backpressure: producer ждёт свободное место;
- `DropOldest`, `DropNewest`, `DropWrite` допустимы, только если потеря задания разрешена;
- `SingleReader/SingleWriter` сообщают каналу модель конкуренции.

## Абстракция очереди

```csharp
public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
        CancellationToken cancellationToken);
}
```

Очередь регистрируется singleton, чтобы HTTP-запросы и worker использовали один канал:

```csharp
services.AddSingleton<IBackgroundTaskQueue>(
    new BackgroundTaskQueue(capacity: 100));
services.AddHostedService<QueueConsumerService>();
```

## Потребитель

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            var workItem = await queue.DequeueAsync(stoppingToken);
            await workItem(stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            break;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка фоновой задачи");
        }
    }
}
```

Исключение одной задачи не должно остановить worker. При shutdown `stoppingToken` позволяет завершиться корректно. Если нужна гарантированная доставка после рестарта, in-memory Channel не подходит — нужна внешняя durable queue.

## Когда применять

- отправка писем/уведомлений после быстрого ответа HTTP;
- ограничение параллелизма внешнего API;
- последовательная обработка;
- внутренняя передача событий.

Нельзя захватывать scoped сервис из HTTP-запроса в делегат, который выполнится позже. Worker должен создать собственный DI scope через `IServiceScopeFactory`.

## Тонкий HTTP-клиент

Задача клиента — знать HTTP-контракт, сериализацию и ошибки, но не бизнес-логику UI.

```text
UI / Use Case
    -> UsersApi (предметные методы)
    -> HttpTransport (HTTP + JSON + ошибки)
    -> HttpClient
```

Единый результат:

```csharp
public readonly struct ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public HttpStatusCode StatusCode { get; init; }
}
```

Категории ошибок: `Network`, `Timeout`, `Http`, `Parse`, `Canceled`. Это позволяет вызывающему коду по-разному показать 404, тайм-аут или отмену.

## Общий `SendAsync`

Шаги транспорта:

1. построить URL и `HttpRequestMessage`;
2. добавить `Accept` и, если нужно, Bearer token;
3. сериализовать request DTO;
4. вызвать `HttpClient.SendAsync`;
5. проверить `IsSuccessStatusCode`;
6. разобрать success/error body;
7. вернуть `ApiResult<T>`.

```csharp
using var response = await client.SendAsync(
    request,
    HttpCompletionOption.ResponseHeadersRead,
    cancellationToken);
```

`ResponseHeadersRead` не буферизует заранее всё тело и полезен для больших ответов.

## Отмена и тайм-аут

```csharp
catch (TaskCanceledException)
    when (cancellationToken.IsCancellationRequested)
{
    // вызывающий код отменил операцию
}
catch (TaskCanceledException)
{
    // timeout HttpClient
}
catch (HttpRequestException)
{
    // DNS, соединение, TLS и другие network errors
}
```

4xx/5xx — это полученный HTTP-ответ, а не обязательно исключение транспорта.

## Типизированный клиент

```csharp
public sealed class UsersApi(HttpTransport transport)
{
    public Task<ApiResult<User>> GetByIdAsync(
        string id,
        CancellationToken ct = default) =>
        transport.GetAsync<User>(
            $"users/{Uri.EscapeDataString(id)}", ct);
}
```

Path segment обязательно экранируется. Для сложной query string лучше builder, а не ручная конкатенация.

## Правила

- `HttpClient` переиспользовать через `IHttpClientFactory`.
- Передавать `CancellationToken` через все уровни.
- Единообразно настроить `JsonSerializerOptions`.
- Отдельно обрабатывать пустой `204`.
- Не логировать Authorization header, Cookie, пароль и токены.
- Не смешивать transport/client layer с UI и бизнес-решениями.
