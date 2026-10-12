# Конспекты по проекту ASP.NET and AJAX

Здесь собран общий конспект по исходникам из `Homework/HW1–HW9` и сохранившимся материалам `ASP6`. Сгенерированные каталоги `bin`/`obj`, карты исходников, двоичные файлы, изображения и полный код сторонних библиотек Bootstrap/jQuery не пересказываются: это зависимости и результаты сборки, а не учебная логика проекта.

## Карта материалов

| Файл | Тема | Откуда взят материал |
| --- | --- | --- |
| [01-aspnet-core-basics.md](01-aspnet-core-basics.md) | Запуск ASP.NET Core, DI, middleware pipeline, маршрутизация | HW1–HW6, HW9 |
| [02-mvc-razor-forms-validation.md](02-mvc-razor-forms-validation.md) | MVC, Razor Views, формы, Model Binding, Data Annotations | HW1–HW3 |
| [03-json-repository-and-ajax.md](03-json-repository-and-ajax.md) | JSON-репозитории, async/await, Fetch API и динамический каталог | HW1, HW3 |
| [04-middleware-request-lifecycle.md](04-middleware-request-lifecycle.md) | Собственный middleware и жизненный цикл HTTP-запроса | HW2 |
| [05-clean-architecture-ef-core-orders.md](05-clean-architecture-ef-core-orders.md) | Domain/Application/Infrastructure/Web/API, EF Core, SQLite, заказы | HW4/HW5 |
| [06-rest-api-reliability.md](06-rest-api-reliability.md) | REST, DTO, Problem Details, FluentValidation, идемпотентность, ETag | HW4/HW5 |
| [07-openapi-swagger-minimal-api.md](07-openapi-swagger-minimal-api.md) | Minimal API и документация OpenAPI/Swagger | HW6 |
| [08-fetch-abortcontroller-races.md](08-fetch-abortcontroller-races.md) | `fetch`, отмена, тайм-ауты, гонки и debounce | HW7 |
| [09-channels-and-http-client.md](09-channels-and-http-client.md) | `Channel<T>`, фоновые задачи и тонкий HTTP-клиент | HW7 |
| [10-ef-and-http-performance.md](10-ef-and-http-performance.md) | N+1, проекции, `IHttpClientFactory`, retry, circuit breaker | HW8 |
| [11-auth-cookie-jwt-bff.md](11-auth-cookie-jwt-bff.md) | Cookie, JWT, refresh rotation, роли и BFF | HW9 |
| [12-security-files-and-mongodb.md](12-security-files-and-mongodb.md) | Безопасная загрузка файлов, STRIDE и примеры MongoDB | ASP6 |

## Эволюция учебных работ

1. **HW1** — MVC-форма, Data Annotations, простой JSON API и AJAX-каталог.
2. **HW2** — собственный middleware и наблюдение полного цикла запроса.
3. **HW3** — CRUD-часть каталога и запись данных в JSON с синхронизацией.
4. **HW4** — многослойное приложение магазина, EF Core/SQLite, MVC и REST API.
5. **HW5** — полноценная optimistic concurrency через ETag и обязательный `If-Match`.
6. **HW6** — отдельный Minimal API с OpenAPI/Swagger.
7. **HW7** — клиентская отмена запросов, борьба с гонками, Channels и HTTP-клиенты.
8. **HW8** — производительность EF Core и устойчивость обращений к внешним API.
9. **HW9** — Cookie + JWT Bearer, роли, refresh tokens и отзыв сессий.
10. **ASP6** — сохранились только загруженные файлы; из них видны темы безопасной загрузки и MongoDB. Исходников самого приложения в каталоге нет.

## Общая схема

```text
Браузер / HTTP-клиент
        |
        v
Middleware pipeline
        |
        +--> MVC Controller --> Use Case --> Repository --> EF Core / JSON
        |
        +--> API Controller / Minimal API --> JSON response
        |
        +--> Razor View --> HTML + CSS + JavaScript
```

Во всех работах повторяются важные идеи: зависимости регистрируются в DI-контейнере, ввод проверяется до выполнения бизнес-операции, асинхронные методы принимают `CancellationToken`, сущности не выдаются наружу напрямую, а ошибки переводятся в понятный HTTP-контракт.
