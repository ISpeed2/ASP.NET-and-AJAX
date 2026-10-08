# Fetch API, AbortController и защита от гонок

## 1. Выполнение HTTP-запроса через `fetch`

`fetch()` возвращает `Promise<Response>`. Успешное выполнение promise означает, что браузер получил HTTP-ответ, но не гарантирует статус 2xx: ответы 404 и 500 не приводят к автоматическому исключению.

```javascript
async function loadUsers(signal) {
  const response = await fetch('/api/users', { signal });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
  }

  return response.json();
}
```

Поэтому HTTP-статус всегда проверяют через `response.ok` или `response.status`. После этого тело можно прочитать один раз методом `json()`, `text()`, `blob()` и т. д.

## 2. Состояния запроса в интерфейсе

Запрос удобно представлять конечным набором состояний:

| Состояние | Значение |
| --- | --- |
| `idle` | запрос ещё не запускался |
| `loading` | запрос выполняется |
| `success` | получены данные |
| `error` | произошла настоящая ошибка |
| `aborted` | запрос отменён; обычно это не ошибка для пользователя |

Хранение одного поля `status` предотвращает противоречивые комбинации вроде `loading === true` одновременно с заполненной ошибкой.

## 3. Отмена запроса

`AbortController` создаёт сигнал отмены. Его передают в `fetch`, а метод `abort()` уведомляет все операции, использующие этот сигнал.

```javascript
const controller = new AbortController();

try {
  const response = await fetch('/api/users', {
    signal: controller.signal,
  });

  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  const data = await response.json();
} catch (error) {
  if (error.name === 'AbortError') {
    // Штатная отмена: сообщение об ошибке пользователю не нужно.
    return;
  }

  throw error;
}

controller.abort();
```

После отмены сигнал остаётся отменённым. Для нового запроса нужен новый `AbortController`.

### Тайм-аут

В современных браузерах тайм-аут можно задать через `AbortSignal.timeout`:

```javascript
const response = await fetch('/api/data', {
  signal: AbortSignal.timeout(5000),
});
```

По истечении времени операция обычно завершается `DOMException` с именем `TimeoutError`. Поддержку этого API нужно учитывать для целевых браузеров; универсальная альтернатива — собственный `AbortController` и `setTimeout`.

### Объединение причин отмены

`AbortSignal.any()` создаёт сигнал, который отменится при первом срабатывании любого исходного сигнала:

```javascript
function request(url, userSignal, timeoutMs = 8000) {
  const timeoutSignal = AbortSignal.timeout(timeoutMs);
  const signal = AbortSignal.any([userSignal, timeoutSignal]);

  return fetch(url, { signal });
}
```

Так можно одновременно поддержать отмену пользователем и ограничение времени ожидания.

## 4. Гонки запросов

Гонка возникает, когда несколько запросов выполняются параллельно, а старый ответ приходит позже нового и перезаписывает актуальные данные. Типичный пример — поиск при быстром вводе текста.

Пусть отправлены запросы `a`, `ab`, `abc`. Если ответ для `a` придёт последним, без защиты интерфейс покажет устаревший результат.

### Подход 1: отмена предыдущего запроса

```javascript
let activeController = null;

async function search(query) {
  activeController?.abort();
  const controller = new AbortController();
  activeController = controller;

  try {
    const response = await fetch(
      `/api/search?q=${encodeURIComponent(query)}`,
      { signal: controller.signal },
    );

    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const data = await response.json();

    if (controller.signal.aborted) return;
    renderResults(data);
  } catch (error) {
    if (error.name === 'AbortError') return;
    renderError(error);
  }
}
```

Отмена экономит сеть и вычислительные ресурсы, но одной отмены может быть недостаточно: ответ или последующая обработка уже могли начаться.

### Подход 2: порядковый номер запроса

```javascript
let requestId = 0;

async function search(query) {
  const myId = ++requestId;
  const response = await fetch(
    `/api/search?q=${encodeURIComponent(query)}`,
  );
  const data = await response.json();

  if (myId !== requestId) return;
  renderResults(data);
}
```

Результат обрабатывается только тогда, когда его номер совпадает с номером последнего запущенного запроса. Устаревшие ошибки нужно фильтровать тем же способом.

### Надёжный вариант: отмена и номер вместе

Лучший практический вариант объединяет оба механизма:

- `AbortController` прекращает ненужную работу;
- счётчик не даёт устаревшему результату изменить UI;
- `loading` сбрасывается только последним запросом.

```javascript
let controller = null;
let latestId = 0;

async function search(query, handlers) {
  controller?.abort();
  controller = new AbortController();

  const signal = controller.signal;
  const myId = ++latestId;
  handlers.onLoading?.(true);

  try {
    const response = await fetch(
      `/api/search?q=${encodeURIComponent(query)}`,
      { signal },
    );
    if (!response.ok) throw new Error(`HTTP ${response.status}`);

    const data = await response.json();
    if (myId !== latestId) return;
    handlers.onSuccess?.(data);
  } catch (error) {
    if (myId !== latestId || error.name === 'AbortError') return;
    handlers.onError?.(error);
  } finally {
    if (myId === latestId) handlers.onLoading?.(false);
  }
}
```

## 5. Debounce и отмена

Debounce откладывает запуск поиска, пока пользователь продолжает ввод. Он уменьшает количество запросов, но не заменяет отмену: предыдущий запрос уже мог быть отправлен.

```javascript
function debounce(fn, delay) {
  let timerId;

  return (...args) => {
    clearTimeout(timerId);
    timerId = setTimeout(() => fn(...args), delay);
  };
}

const debouncedSearch = debounce(search, 300);
```

Для поисковой строки обычно используют оба приёма: debounce сокращает число запусков, а `AbortController` отменяет уже запущенную устаревшую операцию.

## 6. React-хук для загрузки данных

В React контроллер создают внутри `useEffect`, а cleanup отменяет запрос при смене URL или размонтировании компонента.

```javascript
function useFetch(url) {
  const [state, dispatch] = useReducer(reducer, {
    status: 'idle',
    data: null,
    error: null,
  });

  useEffect(() => {
    if (!url) return;

    const controller = new AbortController();
    dispatch({ type: 'start' });

    fetch(url, { signal: controller.signal })
      .then((response) => {
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        return response.json();
      })
      .then((data) => {
        if (!controller.signal.aborted) {
          dispatch({ type: 'success', payload: data });
        }
      })
      .catch((error) => {
        if (error.name !== 'AbortError') {
          dispatch({ type: 'error', payload: error });
        }
      });

    return () => controller.abort();
  }, [url]);

  return state;
}
```

Cleanup защищает от обновления состояния после размонтирования и от результатов запроса со старым URL. Объект `options`, если он используется внутри эффекта, также требует корректного управления зависимостями или мемоизации.

## 7. Защита отправки формы

При двойном клике пользователь может отправить форму несколько раз. На клиенте кнопку блокируют на время запроса:

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

Однако клиентская блокировка не гарантирует единственность операции: повтор возможен из другой вкладки или после сетевого сбоя. Для важных POST-запросов сервер должен поддерживать идемпотентность или ключ идемпотентности.

## 8. Универсальная обёртка над `fetch`

`safeFetch` обычно решает несколько общих задач:

1. проверяет `response.ok`;
2. добавляет тайм-аут и внешний сигнал отмены;
3. различает отмену, тайм-аут, HTTP-ошибку и сетевой сбой;
4. разбирает тело по `Content-Type`;
5. возвращает единый формат результата или выбрасывает нормализованную ошибку.

При проектировании важно заранее выбрать один контракт: либо отмена возвращается как `{ aborted: true }`, либо она выбрасывается как отдельная ошибка. Смешение подходов усложняет вызывающий код.

Сетевая ошибка часто приходит как `TypeError`, но `TypeError` может означать и ошибочные параметры вызова. Не стоит автоматически считать любой `TypeError` отсутствием интернета без дополнительного контекста.

## Краткая памятка

- Всегда проверять `response.ok`.
- Передавать `signal` во все отменяемые запросы.
- Не показывать `AbortError` как обычную ошибку пользователя.
- Для сценария «последний запрос побеждает» применять отмену вместе со счётчиком.
- Использовать debounce для частого ввода, но не считать его защитой от гонок.
- В React отменять запрос в cleanup функции `useEffect`.
- Блокировать повторную отправку формы и дополнительно защищать операцию на сервере.
- Учитывать поддержку `AbortSignal.timeout()` и `AbortSignal.any()` целевыми браузерами.
