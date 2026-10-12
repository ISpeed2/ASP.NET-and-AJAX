# Аутентификация: Cookie, JWT, роли и BFF

Материал из HW9.

## Аутентификация и авторизация

- Authentication: кто пользователь?
- Authorization: что ему разрешено?

Результат аутентификации — `ClaimsPrincipal` с identity и claims. Middleware включаются в порядке:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

## Две схемы в одном приложении

HW9 настраивает policy scheme `Smart`: если есть `Authorization: Bearer`, выбирается JWT, иначе Cookie. Явные политики дополнительно фиксируют, какая схема разрешена:

```csharp
options.AddPolicy("Web", policy => policy
    .AddAuthenticationSchemes("Cookies")
    .RequireAuthenticatedUser());

options.AddPolicy("Api", policy => policy
    .AddAuthenticationSchemes("Bearer")
    .RequireAuthenticatedUser());
```

Admin-варианты добавляют `.RequireRole("admin")`.

## Cookie authentication

Настройки HW9:

- `HttpOnly` — JavaScript не читает Cookie;
- `Secure` — в production только HTTPS;
- `SameSite=Lax` — ограничивает cross-site отправку;
- срок 8 часов и sliding expiration;
- отдельные login/access-denied paths.

После проверки пароля создаются claims и server session ID:

```csharp
var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, user.Username),
    new(ClaimTypes.Name, user.Username),
    new("sid", sessionId)
};

await context.SignInAsync("Cookies", principal, properties);
```

`OnValidatePrincipal` сверяет `sid` с `CookieSessionStore`. Logout удаляет session ID и вызывает `SignOutAsync`, поэтому сессию можно отозвать до истечения ticket.

## CSRF

Браузер прикладывает Cookie автоматически, поэтому POST `/login` и `/logout` защищены antiforgery. Токен формы выдаёт `IAntiforgery`, endpoint помечается `RequireAntiforgeryTokenAttribute`.

`SameSite` помогает, но не заменяет antiforgery для изменяющих Cookie-запросов.

## Пароли

Демонстрационный store применяет `PasswordHasher<UserAccount>`:

```csharp
var result = hasher.VerifyHashedPassword(
    user, user.PasswordHash, password);
```

Пароль нельзя хранить открытым текстом или хешировать обычным быстрым SHA-256. Нужен password hashing с солью и стоимостью: ASP.NET Identity PasswordHasher, Argon2, bcrypt или scrypt.

## JWT access token

JWT содержит header, payload и подпись. Payload кодирован, но не зашифрован: секреты туда не кладут.

HW9 добавляет claims:

- `sub` — пользователь;
- `jti` — ID токена;
- `iat` — время выпуска;
- отдельные `role`.

При проверке обязательно валидируются issuer, audience, signing key, lifetime и алгоритм. Access token живёт 15 минут и передаётся явно:

```http
Authorization: Bearer <access-token>
```

## Refresh token и rotation

Refresh token создаётся криптографическим RNG:

```csharp
var refreshToken = Base64UrlEncoder.Encode(
    RandomNumberGenerator.GetBytes(64));
```

В store сохраняется SHA-256 hash токена, а не raw value. `TakeValid` атомарно удаляет запись при использовании. Это rotation: старый refresh token второй раз не работает, клиент получает новую пару.

In-memory store подходит только для демонстрации. Production требует общей БД/Redis, привязки к сессии/устройству, revoke chain, cleanup, аудит и защиту от reuse.

## Data Protection

Ключи Cookie-ticket сохраняются в `App_Data/keys` и имеют общее application name. В нескольких production-инстансах key ring должен быть общим и защищённым; файл ключа не следует публиковать или коммитить.

## BFF

Backend for Frontend хранит access/refresh token на сервере. Браузер имеет только защищённую Cookie и обращается к BFF; BFF добавляет Bearer при вызове внутреннего API.

Преимущества:

- токены недоступны JavaScript и `localStorage`;
- единая серверная сессия и отзыв;
- проще изолировать внутренние API.

Обязанности BFF: CSRF-защита, безопасная серверная сессия, token refresh, logout/revoke, HTTPS и корректные CORS/headers.

## Endpoint'ы HW9

- `GET/POST /login` — вход через Cookie;
- `GET /profile` — Cookie policy;
- `POST /logout` — отзыв сессии;
- `GET /admin` — Cookie + admin;
- `POST /api/token` — access/refresh token;
- `POST /api/token/refresh` — rotation;
- `GET /api/me` — Bearer;
- `GET /api/admin` — Bearer + admin.

## Что обязательно изменить для production

- убрать ключ JWT из репозитория в secret storage;
- заменить demo users и in-memory stores БД/Identity;
- включить строгий HTTPS;
- добавить rate limiting/lockout на login и token endpoints;
- не логировать пароль, Cookie и токены;
- продумать revoke/logout всех сессий;
- ограничить CORS и настроить security headers;
- не показывать различающиеся ошибки «логин не найден»/«пароль неверен».
