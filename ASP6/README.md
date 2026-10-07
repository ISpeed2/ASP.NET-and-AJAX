# ASP6 — Secure Files MVC

Учебное ASP.NET Core MVC-приложение с моделью угроз STRIDE, security backlog и защищённым API загрузки файлов.

## Запуск

```bash
dotnet run --project src/SecureFilesMvc.Web
```

Откройте адрес, напечатанный в консоли. Демонстрационный API-ключ для страницы загрузки:

```text
demo-secret-key-12345
```

## Защитные меры

- API-key аутентификация и отдельные политики чтения/записи;
- лимит файла 10 МБ, whitelist расширений и проверка magic bytes;
- случайные GUID-имена и хранение в `App_Data/uploads` вне `wwwroot`;
- защита пути через `Path.GetFullPath`;
- CSP, запрет MIME-sniffing и встраивания во frame;
- correlation ID для ответов и журналирования.

`wwwroot/uploads` оставлен только для соответствия учебной структуре и не используется для хранения пользовательских файлов.
