# MVC, Razor, формы и валидация

Материал основан на анкетах HW1/HW2 и каталоге HW3.

## Разделение MVC

- **Model** хранит данные и правила проверки: `QuestionnaireRequest`, `RegistrationRequest`, `Product`.
- **View** — `.cshtml`-шаблон, который строит HTML.
- **Controller** принимает запрос, вызывает нужную логику и выбирает ответ.
- **ViewModel/DTO** отделяет формат экрана или API от доменной сущности.

## GET и POST одной формы

Типичный паттерн:

```csharp
[HttpGet]
public IActionResult Create() => View(new Product());

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Product product, CancellationToken ct)
{
    if (!ModelState.IsValid)
        return View(product);

    var saved = await repository.AddAsync(product, ct);
    return RedirectToAction(nameof(Details), new { id = saved.Id });
}
```

GET показывает пустую форму. POST получает заполненную модель. При ошибках возвращается та же модель, поэтому пользователь видит свои значения и сообщения проверки.

После успешной записи используется redirect. Это вариант паттерна **Post/Redirect/Get**: обновление страницы не повторит POST и не создаст запись дважды.

## Model Binding

Model Binding сопоставляет имена полей формы, route values и query string со свойствами параметра C#.

```html
<input asp-for="Name" />
```

Tag Helper создаёт поле с именем `Name`, а параметр `Product product` получает его значение. Для коллекций имена должны содержать индекс:

```html
<input name="Items[0].ProductId" />
<input name="Items[0].Quantity" />
```

Именно так форма заказа HW4 передаёт список позиций.

## Data Annotations

В моделях используются:

```csharp
[Required(ErrorMessage = "Укажите название товара.")]
[StringLength(100, ErrorMessage = "Название не должно быть длиннее 100 символов.")]
public string Name { get; set; } = string.Empty;

[Range(0.01, 100000000, ErrorMessage = "Цена должна быть больше нуля.")]
public decimal Price { get; set; }

[EmailAddress(ErrorMessage = "Введите корректный email.")]
public string Email { get; set; } = string.Empty;
```

После binding ASP.NET Core записывает ошибки в `ModelState`. Проверка `ModelState.IsValid` обязательна до сохранения данных.

## Razor и Tag Helpers

В представлениях встречаются:

```cshtml
@model Product
<label asp-for="Name"></label>
<input asp-for="Name" />
<span asp-validation-for="Name"></span>
<div asp-validation-summary="ModelOnly"></div>
```

- `@model` задаёт тип `Model`;
- `asp-for` связывает HTML с C#-свойством;
- `asp-validation-for` выводит ошибку поля;
- `asp-validation-summary` выводит общие ошибки;
- `asp-action`, `asp-controller`, `asp-route-id` безопасно строят URL.

Условный вывод и цикл — обычный Razor/C#:

```cshtml
@if (Model.Count == 0)
{
    <p>Каталог пока пуст.</p>
}
else
{
    @foreach (var product in Model)
    {
        <a asp-action="Details" asp-route-id="@product.Id">@product.Name</a>
    }
}
```

Razor по умолчанию HTML-кодирует `@Model.Name`, что снижает риск XSS. Не следует без необходимости использовать `Html.Raw` для пользовательского текста.

## CSRF-защита

В изменяющих MVC-формах есть antiforgery token:

```cshtml
<form asp-action="Create" method="post">
    @Html.AntiForgeryToken()
</form>
```

Контроллер проверяет его через `[ValidateAntiForgeryToken]`. Это защищает от подделки запроса с чужого сайта, когда браузер автоматически прикладывает Cookie.

## ViewModel против Entity

В HW4 форма заказа использует `CreateOrderViewModel`, а не доменный `Order`. Это лучше, потому что:

- наружу открываются только разрешённые поля;
- UI-валидация не загрязняет доменную модель;
- невозможно привязать служебные свойства вроде `Status` или `Total`;
- ViewModel легко менять независимо от базы данных.

## Динамические строки формы

В `Orders/Create.cshtml` JavaScript добавляет строки:

```javascript
const index = tbody.querySelectorAll('tr').length;
row.innerHTML = `
  <input name="Items[${index}].ProductId" />
  <input name="Items[${index}].Quantity" type="number" />`;
```

Тонкость: после удаления строки индексы могут стать разреженными. Для надёжной отправки коллекции полезно перед submit перенумеровать элементы либо применять специальный индексный шаблон.

## UI-слой

Bootstrap даёт сетку, таблицы, кнопки и базовые компоненты. Собственный `site.css` задаёт визуальный стиль анкет и карточек. CSS не участвует в серверной валидации: атрибуты `required`, `min`, `max` удобны для пользователя, но сервер всё равно обязан повторно проверить данные.

## Памятка

- GET показывает форму, POST обрабатывает её.
- При невалидном вводе вернуть тот же `View(model)`.
- После успешной записи применять Post/Redirect/Get.
- Все изменяющие Cookie-запросы защищать antiforgery.
- Для форм и API создавать отдельные ViewModel/DTO.
- Клиентская проверка улучшает UX, но не заменяет серверную.
