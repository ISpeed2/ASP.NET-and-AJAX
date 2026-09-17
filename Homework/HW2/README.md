# Homework 2: ASP.NET Core Middleware

Учебное приложение создано из чистого MVC-шаблона. Оно показывает, как HTTP-запрос проходит через middleware, контроллер, модель и Razor View.

## Запуск

```bash
dotnet run --project Homework/HW2/MiddlewareHomework.csproj --urls http://127.0.0.1:5190
```

Страницы: `/` — форма заявки; `/Home/Lifecycle` — этапы жизненного цикла; после POST `/` — подтверждение и данные формы.

## Жизненный цикл

1. `Program.cs` создаёт приложение и добавляет `RequestLifecycleMiddleware` в конвейер.
2. Middleware получает `HttpContext`, записывает входящий метод, путь и `TraceIdentifier`.
3. `await _next(context)` передаёт запрос следующему компоненту конвейера.
4. Маршрутизация вызывает `HomeController`, который принимает `RegistrationRequest`.
5. Model Binding и Data Annotations проверяют поля формы.
6. Контроллер возвращает `Index.cshtml` или `Success.cshtml`.
7. Middleware получает статус ответа, добавляет заголовки `X-Lifecycle-*` и записывает длительность.

Схема и снимки работы находятся в `docs/`.