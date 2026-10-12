# Безопасная загрузка файлов, STRIDE и MongoDB

В `ASP6` нет исходников приложения: сохранились только файлы в `App_Data/uploads`. Поэтому первая часть — восстановленный конспект по README-подобному загруженному файлу, а вторая — по загруженному практикуму MongoDB.

## Безопасная загрузка файлов

По материалам приложение Secure Files MVC использовало:

- API-key authentication;
- отдельные policies чтения и записи;
- лимит 10 МБ;
- whitelist расширений;
- проверку magic bytes;
- случайные GUID-имена;
- хранение вне `wwwroot`;
- проверку полного пути;
- CSP, `X-Content-Type-Options` и запрет frame;
- correlation ID.

### Почему нельзя доверять имени и Content-Type

Клиент может прислать `malware.exe` как `photo.jpg` и указать любой `Content-Type`. Проверяют одновременно:

1. разрешённое расширение;
2. ожидаемый MIME type;
3. сигнатуру содержимого (magic bytes);
4. размер;
5. при необходимости антивирус/сканер;
6. бизнес-право пользователя на загрузку.

Одна проверка расширения недостаточна.

### Имя и место хранения

Пользовательское имя нельзя использовать как путь. Сервер создаёт собственное имя:

```csharp
var storedName = $"{Guid.NewGuid():N}{safeExtension}";
var candidate = Path.Combine(uploadRoot, storedName);
var fullPath = Path.GetFullPath(candidate);

if (!fullPath.StartsWith(Path.GetFullPath(uploadRoot),
        StringComparison.Ordinal))
    throw new InvalidOperationException("Invalid path");
```

Хранение в `App_Data/uploads`, а не `wwwroot`, предотвращает прямое исполнение/раздачу файла веб-сервером. Скачивание выполняет контроллер после авторизации и выставляет безопасные headers.

### Лимиты и потоковая обработка

Лимит нужно проверять как на уровне reverse proxy/Kestrel, так и приложения. Большие файлы лучше потоково писать в карантин, а не полностью держать в памяти. При ошибке незавершённый файл удаляют.

### Security headers

- CSP ограничивает источники скриптов/стилей/контента;
- `X-Content-Type-Options: nosniff` запрещает браузеру угадывать исполняемый MIME;
- `Content-Disposition: attachment` предлагает скачать файл;
- `frame-ancestors`/`X-Frame-Options` защищает от clickjacking.

### STRIDE

| Угроза | Для загрузки файлов | Типичная защита |
| --- | --- | --- |
| Spoofing | выдача себя за другого клиента | authentication, короткоживущие ключи |
| Tampering | подмена файла/метаданных | hash, access control, immutable metadata |
| Repudiation | отказ от факта загрузки | audit log, user ID, timestamp, correlation ID |
| Information disclosure | чтение чужих файлов | authorization, storage вне web root |
| Denial of service | огромные/частые загрузки | size limits, quotas, rate limiting |
| Elevation of privilege | загрузка исполняемого файла | allowlist, signatures, sandbox/scanner |

### Что заметно в текущем каталоге

В `App_Data/uploads` лежат файлы со случайными GUID-именами — это соответствует описанной защите. Но среди материалов был опубликован демонстрационный API key. Даже демонстрационные значения лучше передавать через user secrets/environment variables и не повторять в конспектах как действующий credential.

## MongoDB: основные запросы из практикума

### Тип данных имеет значение

Число нельзя сравнивать как строку:

```javascript
db.resumes.find({ salary: { $gte: 100000 } })
```

MongoDB сравнивает BSON-типы; строка `"100000"` — не число `100000`.

### Вложенные поля

Для части embedded document используется dot notation:

```javascript
db.resumes.find({ "education.level": "Высшее" })
```

Запрос `{ education: { level: "Высшее" } }` сравнивает весь вложенный документ, включая структуру и порядок полей.

### Массив содержит все значения

```javascript
db.resumes.find({ skills: { $all: ["SQL", "Python"] } })
```

`$all` требует оба навыка. `$in` означал бы «хотя бы одно из значений».

### Несколько условий к одному элементу массива

```javascript
db.resumes.find({
  experience: {
    $elemMatch: {
      company: "Retail Lab",
      months: { $gte: 12 }
    }
  }
})
```

Без `$elemMatch` company может совпасть в одном элементе массива, а months — в другом.

### Диапазон в одном объекте

```javascript
db.resumes.find({
  salary: { $gte: 70000, $lte: 100000 }
})
```

Нельзя дважды писать ключ `salary` в одном JS-объекте: последнее значение перезапишет первое.

### Регулярное выражение

```javascript
db.resumes.find({ position: /python/i })
```

Флаг `i` делает поиск регистронезависимым. Для произвольного пользовательского ввода специальные regex-символы следует экранировать, а тяжёлые regex могут плохо использовать индекс.

### Логические условия

```javascript
db.resumes.find({
  skills: { $all: ["SQL", "Python"] },
  $or: [
    { city: "Санкт-Петербург" },
    { ready_to_move: true }
  ]
})
```

`$exists: true` означает лишь наличие поля и не гарантирует значение `true`.

### Даты: полуоткрытый диапазон

```javascript
db.interviews.find({
  "company.name": "Яндекс",
  stage: { $ne: "скрининг" },
  when: {
    $gte: ISODate("2026-09-15T00:00:00Z"),
    $lt: ISODate("2026-09-26T00:00:00Z")
  }
})
```

Граница `< начало следующего дня` проще и точнее, чем пытаться задать последнее мгновение суток.

### Сравнение полей через `$expr`

```javascript
db.vacancies.find({
  $expr: {
    $lt: ["$salary.to", { $multiply: ["$salary.from", 1.5] }]
  }
})
```

`$expr` позволяет применять aggregation expressions внутри `find`. Для условия «меньше» нужен `$lt`; `$lte` дополнительно включит точное равенство.

Другой пример:

```javascript
db.vacancies.find({
  $expr: {
    $gte: [
      { $subtract: ["$salary.to", "$salary.from"] },
      50000
    ]
  }
})
```

### Наличие поля, `null` и длина массива

```javascript
db.resumes.find({ portfolio: { $exists: false } })
db.resumes.find({ "skills.4": { $exists: true } })
```

Первый запрос находит документы без поля. Запрос `{ field: null }` обычно совпадает и с `null`, и с отсутствующим полем. Второй проверяет наличие пятого элемента, то есть длину массива больше четырёх.

### Выводы из рецензии

- Для массива embedded documents связывать условия через `$elemMatch`.
- Не путать `$all` и `$in`.
- Проверять точный смысл `$exists`: наличие не равно истинности.
- Согласовывать строгое/нестрогое сравнение (`$lt` против `$lte`) с формулировкой задачи.
- Хранить числа и даты правильными BSON-типами.
- Продумывать индексы под частые фильтры, но проверять их через `explain()`.
