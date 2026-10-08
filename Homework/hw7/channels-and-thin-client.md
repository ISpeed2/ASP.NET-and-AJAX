# Channels и тонкий HTTP-клиент в .NET

## 1. `System.Threading.Channels`

`Channel<T>` — потокобезопасная асинхронная очередь для обмена данными между производителями и потребителями. Производитель записывает элементы через `ChannelWriter<T>`, а потребитель читает их через `ChannelReader<T>`.

Главное преимущество канала перед обычной коллекцией — возможность асинхронно ждать появления элемента или свободного места, не блокируя поток.

### Ограниченный канал

```csharp
var options = new BoundedChannelOptions(capacity: 100)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = true,
    SingleWriter = false,
    AllowSynchronousContinuations = false
};

var channel = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
```

`BoundedChannelOptions` задаёт поведение очереди:

- `capacity` — максимальное число элементов;
- `FullMode = Wait` — производитель ждёт, когда освободится место; это создаёт обратное давление (backpressure);
- `SingleReader = true` — читать будет один потребитель;
- `SingleWriter = false` — записывать могут несколько запросов одновременно;
- `AllowSynchronousContinuations = false` — продолжения не выполняются прямо внутри операции записи.

Другие варианты поведения при заполнении: удалить самый старый элемент (`DropOldest`), самый новый (`DropNewest`) или текущую запись (`DropWrite`). Они подходят только тогда, когда потеря части задач допустима.

### Очередь фоновых задач

Удобно скрыть канал за интерфейсом:

```csharp
public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
        CancellationToken cancellationToken);
}
```

Производитель вызывает `Writer.WriteAsync`, а потребитель — `Reader.ReadAsync`. Обе операции принимают `CancellationToken` и работают без активного ожидания.

Очередь регистрируется как singleton, потому что веб-запросы и фоновая служба должны обращаться к одному каналу:

```csharp
services.AddSingleton<IBackgroundTaskQueue>(
    new BackgroundTaskQueue(capacity: 100));
services.AddHostedService<QueueConsumerService>();
```

### Потребитель на основе `BackgroundService`

`BackgroundService.ExecuteAsync` обычно содержит цикл чтения:

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
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка фоновой задачи");
        }
    }
}
```

Исключение одной задачи нужно перехватывать внутри цикла, иначе оно остановит весь обработчик. Отмена через `stoppingToken` при завершении приложения является штатной ситуацией.

Для корректного завершения очереди также можно вызвать `Writer.Complete()` и читать элементы через `ReadAllAsync`, если требуется обработать оставшиеся задания.

### Когда применять Channels

- последовательная фоновая обработка заданий из HTTP-запросов;
- ограничение нагрузки на внешний сервис или базу данных;
- построение схемы producer/consumer;
- передача событий внутри процесса.

Канал хранится только в памяти. Для критичных заданий, которые нельзя потерять при перезапуске приложения, нужна устойчивая внешняя очередь: RabbitMQ, Azure Service Bus, Kafka и т. п.

## 2. Тонкий HTTP-клиент

Тонкий клиент — небольшой типизированный слой над `HttpClient`. Он знает HTTP-контракт конкретного API, но не содержит бизнес-логику приложения и не управляет UI.

Разделение ответственности удобно строить так:

1. `HttpTransport` выполняет запросы, сериализует JSON и классифицирует ошибки.
2. DTO описывают тела запросов и ответов.
3. `UsersApi` предоставляет методы предметной области: `ListAsync`, `GetByIdAsync`, `CreateAsync` и т. д.
4. Вызывающий код решает, как показать данные или ошибку пользователю.

### Единый результат операции

Вместо исключения для каждого неуспешного ответа клиент может возвращать результат-дискриминатор:

```csharp
public readonly struct ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public HttpStatusCode StatusCode { get; init; }
}
```

Ошибки полезно разделять по причинам:

- `Network` — нет соединения, проблема DNS и другие транспортные сбои;
- `Timeout` — превышено время ожидания;
- `Http` — сервер ответил кодом 4xx или 5xx;
- `Parse` — тело ответа не соответствует ожидаемому JSON;
- `Canceled` — операцию отменил вызывающий код.

Это позволяет UI по-разному обрабатывать, например, 404, тайм-аут и отмену пользователем.

### Универсальный транспорт

Методы `GetAsync<T>`, `PostAsync<T>`, `PutAsync<T>`, `PatchAsync<T>` и `DeleteAsync<T>` могут делегировать работу одному `SendAsync<T>`.

Основные шаги `SendAsync<T>`:

1. сформировать URL и `HttpRequestMessage`;
2. добавить `Accept: application/json` и при необходимости Bearer-токен;
3. сериализовать тело запроса в JSON;
4. вызвать `HttpClient.SendAsync` с `CancellationToken`;
5. проверить `IsSuccessStatusCode`;
6. разобрать успешный результат или структурированное тело ошибки;
7. вернуть `ApiResult<T>`.

```csharp
response = await client.SendAsync(
    request,
    HttpCompletionOption.ResponseHeadersRead,
    cancellationToken);
```

`ResponseHeadersRead` возвращает управление после получения заголовков, не дожидаясь полной буферизации тела. Это особенно полезно для больших ответов и потоковой обработки.

### Отмена, тайм-ауты и ошибки

`HttpClient` может выбрасывать `TaskCanceledException` как при отмене, так и при тайм-ауте. Их различают по состоянию переданного токена:

```csharp
catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
{
    // Явная отмена вызывающим кодом
}
catch (TaskCanceledException)
{
    // Тайм-аут HttpClient
}
catch (HttpRequestException exception)
{
    // Сетевая ошибка
}
```

Коды 4xx и 5xx сами по себе не являются транспортными исключениями. Их нужно проверять через `response.IsSuccessStatusCode` и сохранять статус вместе с сообщением сервера.

### Типизированный API

```csharp
public sealed class UsersApi
{
    private readonly HttpTransport transport;

    public Task<ApiResult<User>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default) =>
        transport.GetAsync<User>(
            $"users/{Uri.EscapeDataString(id)}",
            cancellationToken);
}
```

Параметры пути необходимо экранировать через `Uri.EscapeDataString`. Для query string со множеством параметров лучше использовать специальный построитель, а не конкатенацию строк.

### Важные правила

- `HttpClient` нужно переиспользовать; в ASP.NET Core предпочтителен `IHttpClientFactory`.
- `CancellationToken` следует передавать через все уровни вызова.
- DTO должны соответствовать контракту API, а настройки `JsonSerializerOptions` — быть едиными.
- Нельзя считать любой неуспех исключением приложения: ожидаемые HTTP-статусы удобнее возвращать как данные результата.
- Нельзя логировать токены, пароли и другие секреты.
- Пустой успешный ответ (например, `204 No Content`) нужно обрабатывать отдельно, не требуя обязательного объекта `T`.

## Итог

`Channel<T>` решает задачу безопасной асинхронной передачи фоновых работ внутри приложения. Тонкий HTTP-клиент, в свою очередь, изолирует детали сетевого протокола и даёт остальному коду типизированный, отменяемый и предсказуемый интерфейс к внешнему API.
