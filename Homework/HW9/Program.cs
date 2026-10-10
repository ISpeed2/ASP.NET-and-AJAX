using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using HW9.Models;
using HW9.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration
    .GetRequiredSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? throw new InvalidOperationException("Раздел Jwt не настроен");

if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
    throw new InvalidOperationException("Jwt:Key должен содержать не менее 32 байт");

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<DemoUserStore>();
builder.Services.AddSingleton<CookieSessionStore>();
builder.Services.AddSingleton<RefreshTokenStore>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")))
    .SetApplicationName("HW9");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = AuthSchemes.Smart;
        options.DefaultChallengeScheme = AuthSchemes.Smart;
    })
    .AddPolicyScheme(AuthSchemes.Smart, AuthSchemes.Smart, options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.Authorization.ToString()
                .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? AuthSchemes.Bearer
                : AuthSchemes.Cookies;
    })
    .AddCookie(AuthSchemes.Cookies, options =>
    {
        options.Cookie.Name = ".HW9.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/forbidden";
        options.Events.OnValidatePrincipal = async context =>
        {
            var sessionId = context.Principal?.FindFirstValue("sid");
            var sessions = context.HttpContext.RequestServices
                .GetRequiredService<CookieSessionStore>();

            if (sessionId is null || !sessions.IsActive(sessionId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(AuthSchemes.Cookies);
            }
        };
    })
    .AddJwtBearer(AuthSchemes.Bearer, options =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.Web, policy => policy
        .AddAuthenticationSchemes(AuthSchemes.Cookies)
        .RequireAuthenticatedUser());
    options.AddPolicy(AuthPolicies.WebAdmin, policy => policy
        .AddAuthenticationSchemes(AuthSchemes.Cookies)
        .RequireRole("admin"));
    options.AddPolicy(AuthPolicies.Api, policy => policy
        .AddAuthenticationSchemes(AuthSchemes.Bearer)
        .RequireAuthenticatedUser());
    options.AddPolicy(AuthPolicies.ApiAdmin, policy => policy
        .AddAuthenticationSchemes(AuthSchemes.Bearer)
        .RequireRole("admin"));
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/", () => Results.Content(Html.Page("HW9 · Аутентификация", """
    <main class="card">
      <p class="eyebrow">ASP.NET CORE · COOKIE + JWT</p>
      <h1>Две схемы аутентификации</h1>
      <p>Cookie используется для браузерной сессии, Bearer-токен — для API.</p>
      <nav>
        <a class="button" href="/login">Войти через Cookie</a>
        <a class="button secondary" href="/api/help">Открыть API-инструкцию</a>
      </nav>
      <p class="hint">Демо: alice / p@ss (admin), bob / p@ss (user)</p>
    </main>
    """), "text/html; charset=utf-8"));

app.MapGet("/login", (HttpContext context, IAntiforgery antiforgery) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
        return Results.Redirect("/profile");

    var token = antiforgery.GetAndStoreTokens(context);
    var error = context.Request.Query.ContainsKey("error")
        ? "<p class=\"error\">Неверный логин или пароль</p>"
        : string.Empty;
    var body = $$"""
        <main class="card">
          <p class="eyebrow">COOKIE AUTHENTICATION</p>
          <h1>Вход</h1>
          {{error}}
          <form method="post" action="/login">
            <input type="hidden" name="{{token.FormFieldName}}" value="{{HtmlEncoder.Default.Encode(token.RequestToken!)}}">
            <label>Логин<input name="Username" autocomplete="username" required></label>
            <label>Пароль<input name="Password" type="password" autocomplete="current-password" required></label>
            <label class="check"><input name="RememberMe" type="checkbox" value="true"> Запомнить меня</label>
            <button class="button" type="submit">Войти</button>
          </form>
        </main>
        """;
    return Results.Content(Html.Page("Вход", body), "text/html; charset=utf-8");
});

app.MapPost("/login", async (
    HttpContext context,
    [FromForm] LoginForm request,
    DemoUserStore users,
    CookieSessionStore sessions) =>
{
    var user = users.Validate(request.Username, request.Password);
    if (user is null)
        return Results.Redirect("/login?error=1");

    var sessionId = Guid.NewGuid().ToString("N");
    sessions.Add(sessionId);

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Username),
        new(ClaimTypes.Name, user.Username),
        new("sid", sessionId)
    };
    claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

    var principal = new ClaimsPrincipal(
        new ClaimsIdentity(claims, AuthSchemes.Cookies));
    await context.SignInAsync(AuthSchemes.Cookies, principal,
        new AuthenticationProperties
        {
            IsPersistent = request.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(request.RememberMe ? 24 * 7 : 8),
            AllowRefresh = true
        });

    return Results.Redirect("/profile");
}).WithMetadata(new RequireAntiforgeryTokenAttribute());

app.MapGet("/profile", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    var name = HtmlEncoder.Default.Encode(context.User.Identity?.Name ?? "");
    var roles = string.Join(", ", context.User.FindAll(ClaimTypes.Role)
        .Select(claim => HtmlEncoder.Default.Encode(claim.Value)));
    var body = $$"""
        <main class="card">
          <p class="eyebrow">AUTHENTICATED</p>
          <h1>Привет, {{name}}</h1>
          <p>Роли: <strong>{{roles}}</strong></p>
          <nav><a class="button secondary" href="/admin">Проверить admin-раздел</a></nav>
          <form method="post" action="/logout">
            <input type="hidden" name="{{token.FormFieldName}}" value="{{HtmlEncoder.Default.Encode(token.RequestToken!)}}">
            <button class="button danger" type="submit">Выйти и отозвать сессию</button>
          </form>
        </main>
        """;
    return Results.Content(Html.Page("Профиль", body), "text/html; charset=utf-8");
}).RequireAuthorization(AuthPolicies.Web);

app.MapPost("/logout", async (HttpContext context, CookieSessionStore sessions) =>
{
    var sessionId = context.User.FindFirstValue("sid");
    if (sessionId is not null)
        sessions.Revoke(sessionId);

    await context.SignOutAsync(AuthSchemes.Cookies);
    return Results.Redirect("/");
}).RequireAuthorization(AuthPolicies.Web)
  .WithMetadata(new RequireAntiforgeryTokenAttribute());

app.MapGet("/admin", () => Results.Content(
    Html.Page("Admin", "<main class=\"card\"><h1>Admin area</h1><p>Доступ разрешён.</p></main>"),
    "text/html; charset=utf-8"))
    .RequireAuthorization(AuthPolicies.WebAdmin);

app.MapGet("/forbidden", () => Results.Content(
    Html.Page("Доступ запрещён", "<main class=\"card\"><h1>403</h1><p>Недостаточно прав.</p></main>"),
    "text/html; charset=utf-8"));

app.MapGet("/api/help", () => Results.Ok(new
{
    credentials = new[] { "alice / p@ss (admin)", "bob / p@ss (user)" },
    token = "POST /api/token",
    refresh = "POST /api/token/refresh",
    me = "GET /api/me with Authorization: Bearer <token>",
    admin = "GET /api/admin with an admin token"
}));

app.MapPost("/api/token", (
    TokenRequest request,
    DemoUserStore users,
    JwtTokenService tokens) =>
{
    var user = users.Validate(request.Username, request.Password);
    return user is null ? Results.Unauthorized() : Results.Ok(tokens.Create(user));
});

app.MapPost("/api/token/refresh", (
    RefreshRequest request,
    RefreshTokenStore refreshTokens,
    DemoUserStore users,
    JwtTokenService tokens) =>
{
    var entry = refreshTokens.TakeValid(request.RefreshToken);
    var user = entry is null ? null : users.Find(entry.Username);
    return user is null ? Results.Unauthorized() : Results.Ok(tokens.Create(user));
});

app.MapGet("/api/me", (ClaimsPrincipal user) => Results.Ok(new
{
    subject = user.FindFirstValue("sub"),
    roles = user.FindAll("role").Select(claim => claim.Value)
})).RequireAuthorization(AuthPolicies.Api);

app.MapGet("/api/admin", () => Results.Ok(new { message = "admin area" }))
    .RequireAuthorization(AuthPolicies.ApiAdmin);

app.Run();

internal static class Html
{
    public static string Page(string title, string body) => $$"""
        <!doctype html>
        <html lang="ru">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{{HtmlEncoder.Default.Encode(title)}} · HW9</title>
          <link rel="stylesheet" href="/site.css">
        </head>
        <body>{{body}}</body>
        </html>
        """;
}
