# HW9 — Cookie и JWT Bearer authentication

Учебное ASP.NET Core-приложение демонстрирует две схемы аутентификации:

- Cookie для браузерных страниц;
- JWT Bearer для API;
- автоматический выбор схемы через policy scheme;
- роли `user` и `admin`;
- antiforgery-защиту форм;
- отзыв Cookie-сессии;
- одноразовую ротацию refresh-токена.

## Запуск

```bash
dotnet run --project Homework/HW9/HW9.csproj
```

Откройте адрес из консоли. Демонстрационные пользователи:

| Логин | Пароль | Роли |
| --- | --- | --- |
| `alice` | `p@ss` | `user`, `admin` |
| `bob` | `p@ss` | `user` |

## Получение JWT

```bash
curl -X POST http://localhost:5000/api/token \
  -H 'Content-Type: application/json' \
  -d '{"username":"alice","password":"p@ss"}'
```

Далее access token передаётся в заголовке:

```http
Authorization: Bearer <access_token>
```

Демо-ключ подписи находится в `appsettings.json` только для учебного запуска.
В реальном приложении его нужно заменить через переменную окружения `Jwt__Key`
или безопасное хранилище секретов.
