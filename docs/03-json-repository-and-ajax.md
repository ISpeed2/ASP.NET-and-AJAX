# JSON-репозитории и AJAX-каталог

## Репозиторий как абстракция

В HW1 контроллер зависит от интерфейса, а не от файла:

```csharp
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByCategoryAsync(string category, CancellationToken ct = default);
}
```

Это позволяет позже заменить JSON на EF Core, не переписывая контроллер. Реализация регистрируется в DI:

```csharp
builder.Services.AddSingleton<IProductRepository, JsonProductRepository>();
```

## Чтение JSON

Путь строится относительно корня приложения:

```csharp
_filePath = Path.Combine(env.ContentRootPath, "Data", "products.json");
```

Асинхронная десериализация:

```csharp
await using var stream = File.OpenRead(_filePath);
var items = await JsonSerializer.DeserializeAsync<List<Product>>(
    stream,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
    ct);
```

`PropertyNameCaseInsensitive` позволяет читать JSON независимо от регистра имён. Если файла нет или JSON содержит `null`, код возвращает пустой список.

## Запись и синхронизация в HW3

HW3 добавляет сохранение товара и использует `SemaphoreSlim`:

```csharp
await _mutex.WaitAsync(ct);
try
{
    var products = await LoadAsync(ct);
    product.Id = products.Count == 0 ? 1 : products.Max(x => x.Id) + 1;
    products.Add(product);

    await using var stream = File.Create(_filePath);
    await JsonSerializer.SerializeAsync(stream, products, _options, ct);
}
finally
{
    _mutex.Release();
}
```

Зачем mutex: два запроса иначе могут одновременно прочитать один список, назначить одинаковый ID и перезаписать изменения друг друга.

Ограничения подхода:

- блокировка действует только внутри одного процесса;
- запись `File.Create` не атомарна при падении;
- весь файл читается/перезаписывается на каждую операцию;
- нет транзакций и индексов.

Для учебного проекта это допустимо, но в реальном многопоточном/многоэкземплярном приложении нужна БД или атомарная запись через временный файл и замену.

## API-контроллер

HW1 возвращает DTO, а не исходную сущность:

```csharp
[Route("api/products")]
public class ProductsController : Controller
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? category,
        CancellationToken ct)
    {
        var items = string.IsNullOrWhiteSpace(category)
            ? await repo.GetAllAsync(ct)
            : await repo.GetByCategoryAsync(category, ct);

        return Json(items.Select(ToDto));
    }
}
```

Маршруты:

- `GET /api/products` — все товары;
- `GET /api/products?category=Мебель` — фильтр;
- `GET /api/products/3` — один товар;
- отсутствующий товар — `404 Not Found` с JSON-описанием.

## AJAX через `fetch`

Каталог загружается без перезагрузки страницы:

```javascript
const response = await fetch(url, {
  headers: { Accept: 'application/json' }
});

if (!response.ok) {
  throw new Error(`HTTP ${response.status}`);
}

const products = await response.json();
render(products);
```

Важно: `fetch` не выбрасывает исключение на 404/500, поэтому `response.ok` проверяется вручную.

## Безопасный рендеринг

Код клонирует `<template>` и присваивает данные через `textContent`:

```javascript
const node = template.content.cloneNode(true);
node.querySelector('[data-field="name"]').textContent = product.name;
fragment.appendChild(node);
root.replaceChildren(fragment);
```

`textContent` не интерпретирует товар как HTML и безопаснее, чем вставка пользовательских данных в `innerHTML`. `DocumentFragment` позволяет собрать узлы до одного обновления DOM.

## Фильтры и делегирование событий

```javascript
document.querySelectorAll('.filter-btn').forEach(button => {
  button.addEventListener('click', () =>
    load(button.dataset.category || null));
});

root.addEventListener('click', event => {
  const button = event.target.closest('.details-btn');
  if (!button) return;
  // загрузить подробности
});
```

Второй обработчик висит на контейнере, а не на каждой динамически созданной кнопке. Это **делегирование событий**.

Query parameter кодируется:

```javascript
`${endpoint}?category=${encodeURIComponent(category)}`
```

Иначе пробелы, `&`, `?` и национальные символы могут сломать URL.

## Что можно улучшить

- отменять предыдущий запрос через `AbortController`;
- показывать отдельные состояния `loading`, `empty`, `error`;
- не использовать `alert` для деталей товара, а открыть доступный диалог;
- кешировать JSON на сервере, если файл редко меняется;
- для записи перейти на БД и транзакции.
