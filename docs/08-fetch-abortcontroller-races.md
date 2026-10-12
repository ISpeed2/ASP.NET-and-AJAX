# Fetch API, AbortController и гонки запросов

Материал из `Homework/hw7/fetch-and-abortcontroller.md` и AJAX-каталога HW1.

## Базовый `fetch`

```javascript
async function loadUsers(signal) {
  const response = await fetch('/api/users', { signal });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
  }

  return response.json();
}
```

Promise `fetch` выполняется успешно даже при 404/500: это полученный HTTP-ответ, а не сетевой сбой. Поэтому нужно проверять `response.ok`. Тело можно прочитать только один раз как `json()`, `text()`, `blob()` и т. п.

## Состояния UI

Полезная модель:

| Статус | Значение |
| --- | --- |
| `idle` | запрос ещё не запускался |
| `loading` | идёт загрузка |
| `success` | данные получены |
| `error` | настоящая ошибка |
| `aborted` | штатная отмена |

Одно поле `status` не допускает противоречивых состояний вроде одновременных `loading=true` и `error=true`.

## Отмена

```javascript
const controller = new AbortController();

try {
  const response = await fetch('/api/users', {
    signal: controller.signal
  });
  if (!response.ok) throw new Error(`HTTP ${response.status}`);
} catch (error) {
  if (error.name === 'AbortError') return;
  throw error;
}

controller.abort();
```

После `abort()` сигнал навсегда отменён; для следующего запроса создаётся новый controller. Отмену обычно не показывают пользователю как ошибку.

Современные API:

```javascript
const timeout = AbortSignal.timeout(5000);
const signal = AbortSignal.any([userController.signal, timeout]);
await fetch(url, { signal });
```

Нужно проверить поддержку целевыми браузерами или сделать fallback через `setTimeout`.

## Гонка результатов

При поиске отправляются `a`, `ab`, `abc`. Если старый `a` завершится последним, он может перезаписать актуальный результат. Отмена снижает лишнюю работу, но дополнительный request ID гарантирует корректность UI:

```javascript
let controller = null;
let latestId = 0;

async function search(query, handlers) {
  controller?.abort();
  controller = new AbortController();

  const myId = ++latestId;
  const signal = controller.signal;
  handlers.onLoading?.(true);

  try {
    const response = await fetch(
      `/api/search?q=${encodeURIComponent(query)}`,
      { signal });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);

    const data = await response.json();
    if (myId === latestId) handlers.onSuccess?.(data);
  } catch (error) {
    if (myId !== latestId || error.name === 'AbortError') return;
    handlers.onError?.(error);
  } finally {
    if (myId === latestId) handlers.onLoading?.(false);
  }
}
```

Отмена экономит ресурсы, ID не позволяет устаревшему continuation изменить интерфейс.

## Debounce

```javascript
function debounce(fn, delay) {
  let timerId;
  return (...args) => {
    clearTimeout(timerId);
    timerId = setTimeout(() => fn(...args), delay);
  };
}
```

Debounce уменьшает число запусков при быстром вводе. Он не заменяет AbortController: старый запрос мог уже начаться.

## React cleanup

Controller создают внутри `useEffect`, а cleanup отменяет запрос при смене URL или размонтировании:

```javascript
useEffect(() => {
  const controller = new AbortController();

  fetch(url, { signal: controller.signal })
    .then(response => {
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      return response.json();
    })
    .then(data => {
      if (!controller.signal.aborted) setData(data);
    })
    .catch(error => {
      if (error.name !== 'AbortError') setError(error);
    });

  return () => controller.abort();
}, [url]);
```

## Защита submit

```javascript
if (isSubmitting) return;
isSubmitting = true;
button.disabled = true;

try {
  await fetch('/api/submit', { method: 'POST', body: formData });
} finally {
  isSubmitting = false;
  button.disabled = false;
}
```

Это UX-защита от двойного клика, но не гарантия единственности операции. Для важных POST сервер должен поддерживать `Idempotency-Key`.

## Контракт обёртки `safeFetch`

Общая обёртка должна единообразно:

- проверять `response.ok`;
- применять внешний signal и timeout;
- различать abort, timeout, HTTP и network error;
- разбирать тело по `Content-Type`;
- корректно обрабатывать `204 No Content`;
- возвращать один предсказуемый тип результата.

Не следует считать каждый `TypeError` отсутствием интернета: он бывает и при неверных параметрах вызова.
